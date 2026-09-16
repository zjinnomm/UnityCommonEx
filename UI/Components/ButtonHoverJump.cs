using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public class ButtonHoverJump : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public Button Button;
        public RectTransform Visual;
        public float Height = 5f;
        public float Duration = 0.2f;
        public AnimationCurve JumpCurve;
        public float PressDepth = 3f;
        public float PressDuration = 0.07f;

        Vector2 restPosition;
        float startOffset;
        float offset;
        float targetOffset;
        float elapsed;
        bool pointerInside;
        bool pointerDown;
        bool pressed;
        float transitionDuration;

        void OnEnable()
        {
            if (Visual != null)
                restPosition = Visual.anchoredPosition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                Button != null && Button.IsActive() && Button.IsInteractable())
                pointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                pointerDown = false;
        }

        void Update()
        {
            if (Visual == null || Button == null)
                return;

            bool interactable = Button.IsActive() && Button.IsInteractable();
            if (!interactable)
                pointerDown = false;
            bool shouldRaise = pointerInside && interactable;
            bool shouldPress = shouldRaise && pointerDown;
            float nextOffset = shouldRaise ? Height - (shouldPress ? PressDepth : 0f) : 0f;
            if (nextOffset != targetOffset || shouldPress != pressed)
            {
                pressed = shouldPress;
                startOffset = offset;
                targetOffset = nextOffset;
                transitionDuration = pressed ? PressDuration : Duration;
                elapsed = 0f;
            }

            if (offset == targetOffset && elapsed >= transitionDuration)
                return;
            elapsed += Time.unscaledDeltaTime;
            float progress = transitionDuration > 0f ? Mathf.Clamp01(elapsed / transitionDuration) : 1f;
            float blend = pressed ? 1f - (1f - progress) * (1f - progress) :
                JumpCurve != null && JumpCurve.length > 0 ? JumpCurve.Evaluate(progress) : progress;
            offset = progress >= 1f ? targetOffset : Mathf.LerpUnclamped(startOffset, targetOffset, blend);
            Visual.anchoredPosition = restPosition + Vector2.up * offset;
        }

        void OnDisable()
        {
            if (Visual != null)
                Visual.anchoredPosition = restPosition;
            pointerInside = false;
            pointerDown = false;
            pressed = false;
            startOffset = offset = targetOffset = elapsed = 0f;
        }
    }
}
