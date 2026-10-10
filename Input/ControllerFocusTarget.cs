using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public interface IControllerFocusAvailability { bool HasControllerContent { get; } }
    public interface IControllerFocusHandler { void SetControllerFocus(bool active, PointerEventData pointer); }
    public interface IControllerReorderItem
    {
        bool BeginControllerReorder();
        void MoveControllerReorder(int direction);
        void FinishControllerReorder(bool commit);
        bool IsControllerReordering { get; }
    }

    [DisallowMultipleComponent]
    public class ControllerFocusTarget : MonoBehaviour
    {
        public RectTransform Rect;
        public bool Focusable = true;
        public GameObject EventTarget;
        public GameObject[] HoverTargets;
        public Selectable Control;
        public MonoBehaviour Item;
        public Transform RegionRoot;
        public bool CancelAction;
        public int CancelPriority;
        public bool BackgroundAction;
        public bool UsePointerClick;
        public int InitialPriority;
        public float SliderStep;
        public bool IsFocused { get; private set; }
        public static readonly List<ControllerFocusTarget> ActiveTargets = new List<ControllerFocusTarget>();

        protected virtual void OnEnable() { if (!ActiveTargets.Contains(this)) ActiveTargets.Add(this); }
        protected virtual void OnDisable()
        {
            ActiveTargets.Remove(this);
            if (!IsFocused) return;
            IsFocused = false;
            OnFocus(false);
        }

        protected virtual void OnFocus(bool focused) { }

        public bool IsAvailable
        {
            get
            {
                if (!isActiveAndEnabled || Rect == null || BackgroundAction) return false;
                if (Control != null && (!Control.IsActive() || !Control.IsInteractable())) return false;
                foreach (var group in GetComponentsInParent<CanvasGroup>())
                {
                    if (!group.interactable || group.alpha < 0.01f) return false;
                    if (group.ignoreParentGroups) break;
                }
                if (Item is IControllerFocusAvailability content && !content.HasControllerContent) return false;
                return true;
            }
        }

        public bool CanFocus
        {
            get
            {
                if (!Focusable || !IsAvailable) return false;
                foreach (var group in GetComponentsInParent<ControllerFocusGroup>())
                    if (group.isActiveAndEnabled && !group.Focusable) return false;
                return true;
            }
        }

        public int Layer
        {
            get
            {
                int layer = 0;
                foreach (var canvas in GetComponentsInParent<Canvas>())
                    layer = Mathf.Max(layer, canvas.sortingOrder);
                return layer;
            }
        }

        public Vector2 ScreenPosition
        {
            get
            {
                var canvas = Rect.GetComponentInParent<Canvas>();
                return RectTransformUtility.WorldToScreenPoint(
                    canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null,
                    Rect.TransformPoint(Rect.rect.center));
            }
        }

        public Rect ScreenRect
        {
            get
            {
                var corners = new Vector3[4];
                Rect.GetWorldCorners(corners);
                var canvas = Rect.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 max = min;
                for (int i = 1; i < 4; i++)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                return UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }

        public void Focus(bool active, PointerEventData pointer)
        {
            if (IsFocused != active)
            {
                IsFocused = active;
                OnFocus(active);
            }
            if (Item is IControllerFocusHandler handler) handler.SetControllerFocus(active, pointer);
            if (EventTarget != null && EventTarget.activeInHierarchy)
            {
                if (active) ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerEnterHandler);
                else ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerExitHandler);
            }
            if (HoverTargets == null) return;
            foreach (var target in HoverTargets)
            {
                if (target == null || !target.activeInHierarchy || target == EventTarget) continue;
                if (active) ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
                else ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler);
            }
        }

        public void Activate(PointerEventData pointer)
        {
            if (!IsAvailable) return;
            if (Control is Button button) button.onClick.Invoke();
            else if (Control is Toggle toggle) toggle.isOn = !toggle.isOn;
            else if (Control != null) ExecuteEvents.Execute(Control.gameObject, pointer, ExecuteEvents.submitHandler);
            else if (UsePointerClick)
                ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerClickHandler);
            else if (Item is UIListItemController listItem) listItem.OnClick();
            else if (EventTarget != null) ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerClickHandler);
        }

        public void EnsureVisible()
        {
            var scroll = GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, Rect);
            Rect viewport = scroll.viewport.rect;
            Vector2 offset = Vector2.zero;
            if (scroll.vertical)
            {
                if (bounds.max.y > viewport.yMax) offset.y = viewport.yMax - bounds.max.y;
                else if (bounds.min.y < viewport.yMin) offset.y = viewport.yMin - bounds.min.y;
            }
            if (scroll.horizontal)
            {
                if (bounds.min.x < viewport.xMin) offset.x = viewport.xMin - bounds.min.x;
                else if (bounds.max.x > viewport.xMax) offset.x = viewport.xMax - bounds.max.x;
            }
            scroll.StopMovement();
            Vector3 world = scroll.viewport.TransformVector(offset);
            scroll.content.anchoredPosition += (Vector2)scroll.content.parent.InverseTransformVector(world);
        }
    }
}
