using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityCommonEx
{

    public class GeneralOptionDialogController : NodeController
    {

        public TMP_Text MessageText;
        public Transform OptionButtonRoot;
        public GameObject OptionButtonPrefab;

        List<RichButtonController> optionButtons = new List<RichButtonController>();
        List<Action> callbacks = new List<Action>();

        public struct Option
        {
            public string Title;
            public string Subtitle;
            public Sprite Icon;
            public Action Callback;
            public bool Interactable;
        }

        public void Set(string message, IList<Option> options)
        {
            Clear();
            MessageText.text = message;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                Option option = options[i];
                RichButtonController button = Create<RichButtonController>(OptionButtonPrefab, OptionButtonRoot);
                button.Set(option.Title, () => { OnButtonClick(index); }, option.Icon, option.Subtitle, option.Interactable);
                optionButtons.Add(button);
                callbacks.Add(option.Callback);
            }
        }

        void OnButtonClick(int index)
        {
            if (index < callbacks.Count)
            {
                callbacks[index]?.Invoke();
            }
            Deactivate();
        }

        public void Clear()
        {
            MessageText.text = null;
            foreach (var button in optionButtons)
            {
                if (button != null)
                {
                    button.Destroy();
                }
            }
            optionButtons.Clear();
            callbacks.Clear();
        }

    }

}