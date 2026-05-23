using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityCommonEx
{
    /// <summary>
    /// 胶囊形双态开关：一条轨道（背景）+ 可点击的旋钮；关时旋钮靠轨左，开时靠轨右。
    /// 约定：轨道与旋钮为 <see cref="RectTransform"/>，旋钮为轨道的子物体，二者锚点均为中心 (0.5, 0.5)，轨道轴心为局部原点，便于用 <see cref="RectTransform.anchoredPosition"/> 沿 X 滑动。
    /// </summary>
    [DisallowMultipleComponent]
    public class CapsuleSwitchToggle : MonoBehaviour
    {
        [Tooltip("轨道矩形；为空则使用本物体上的 RectTransform")]
        public RectTransform Track;

        [Tooltip("旋钮；应为 Track 的子物体")]
        public RectTransform Knob;

        [Tooltip("旋钮上的 Button；为空则在 Knob 上 GetComponent<Button>()")]
        public Button KnobButton;

        [Tooltip("轨道图片；为空则在 Track 上 GetComponent<Image>()")]
        public Image TrackImage;

        [Tooltip("旋钮图片；为空则在 Knob 上 GetComponent<Image>()")]
        public Image KnobImage;

        [Tooltip("旋钮与轨道左右端之间的留白")]
        [SerializeField]
        float endPadding = 6f;

        [Tooltip("关闭或不可用时使用的颜色")]
        [SerializeField]
        Color offColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);

        [SerializeField]
        bool m_IsOn;

        /// <summary>与 <see cref="UnityEngine.UI.Toggle.onValueChanged"/> 类似，参数为当前是否为「开」。</summary>
        public UnityEvent<bool> onValueChanged = new UnityEvent<bool>();

        public bool isOn
        {
            get => m_IsOn;
            set => Set(value, true);
        }

        /// <summary>为 false 时旋钮按钮不可点（与 <see cref="UnityEngine.UI.Selectable.interactable"/> 一致）。</summary>
        public bool interactable
        {
            get => KnobButton == null || KnobButton.interactable;
            set
            {
                if (KnobButton != null)
                {
                    KnobButton.interactable = value;
                }

                ApplyVisualState();
            }
        }

        void Awake()
        {
            if (Track == null)
                Track = transform as RectTransform;
            if (KnobButton == null && Knob != null)
                KnobButton = Knob.GetComponent<Button>();
            if (TrackImage == null && Track != null)
                TrackImage = Track.GetComponent<Image>();
            if (KnobImage == null && Knob != null)
                KnobImage = Knob.GetComponent<Image>();
            if (KnobButton != null)
            {
                KnobButton.transition = Selectable.Transition.None;
                KnobButton.onClick.AddListener(OnKnobClicked);
            }
        }

        void OnDestroy()
        {
            if (KnobButton != null)
                KnobButton.onClick.RemoveListener(OnKnobClicked);
        }

        void OnEnable()
        {
            ApplyVisualState();
        }

        void Start()
        {
            ApplyVisualState();
        }

        void OnRectTransformDimensionsChange()
        {
            ApplyVisualState();
        }

        void OnKnobClicked()
        {
            Set(!m_IsOn, true);
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }

        void Set(bool value, bool notify)
        {
            if (m_IsOn == value)
            {
                ApplyVisualState();
                return;
            }
            m_IsOn = value;
            ApplyVisualState();
            if (notify)
                onValueChanged?.Invoke(m_IsOn);
        }

        void ApplyVisualState()
        {
            ApplyKnobPosition();
            ApplyColors();
        }

        void ApplyKnobPosition()
        {
            if (Track == null || Knob == null)
                return;
            float halfTrack = Track.rect.width * 0.5f;
            float halfKnob = Knob.rect.width * 0.5f;
            float x = m_IsOn
                ? halfTrack - endPadding - halfKnob
                : -halfTrack + endPadding + halfKnob;
            Vector2 p = Knob.anchoredPosition;
            p.x = x;
            Knob.anchoredPosition = p;
        }

        void ApplyColors()
        {
            bool isInteractable = interactable;

            if (TrackImage != null)
                TrackImage.color = isInteractable ? Color.white : offColor;

            if (KnobImage != null)
                KnobImage.color = !isInteractable || !m_IsOn ? offColor : Color.white;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (TrackImage == null && Track != null)
                    TrackImage = Track.GetComponent<Image>();
                if (KnobImage == null && Knob != null)
                    KnobImage = Knob.GetComponent<Image>();

                ApplyVisualState();
            }
        }
#endif
    }
}
