using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    [RequireComponent(typeof(Button))]
    public class ButtonSFXPlayer : MonoBehaviour
    {
        public Button Button;
        public string SFXName;
        public float Pitch = 1f;

        [Range(0f, 1f)]
        public float Volume = 1f;

        private SfxOncePerFrame sfx;

        private void OnEnable()
        {
            if (Button != null)
                Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (Button != null)
                Button.onClick.RemoveListener(OnButtonClicked);
        }

        public bool Play()
        {
            return sfx.TryPlay(SFXName, Pitch, Volume);
        }

        private void OnButtonClicked()
        {
            Play();
        }
    }
}
