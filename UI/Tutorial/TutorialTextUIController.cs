using TMPro;
using UnityEngine;

namespace UnityCommonEx
{
    [RequireComponent(typeof(TMP_Text))]
    public class TutorialTextUIController : NodeController
    {
        public TMP_Text Text;

        public RectTransform RectTransform => transform as RectTransform;

        public TMP_Text TextComponent => Text;

        protected override void OnInit()
        {
            base.OnInit();
            if (Text == null)
            {
                Text = GetComponent<TMP_Text>();
            }
        }

        protected override void OnDeactivate()
        {
            ClearText();
            base.OnDeactivate();
        }

        public virtual void SetText(string text)
        {
            if (Text == null)
            {
                return;
            }

            Text.maxVisibleCharacters = int.MaxValue;
            Text.text = text ?? string.Empty;
        }

        public virtual void ClearText()
        {
            if (Text == null)
            {
                return;
            }

            Text.maxVisibleCharacters = int.MaxValue;
            Text.text = string.Empty;
        }
    }
}
