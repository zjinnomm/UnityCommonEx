using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public interface IControllerFocusAvailability { bool HasControllerContent { get; } }
    public interface IControllerFocusHandler { void SetControllerFocus(bool active, PointerEventData pointer); }
    public interface IControllerSubmitAvailability { bool CanControllerSubmit { get; } }
    public interface IControllerReorderItem
    {
        bool CanControllerReorder { get; }
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
        [Tooltip("Prefer nearby targets over column alignment when navigating a staggered grid.")]
        public bool PreferNearestInDirection;
        public bool BackgroundAction;
        public bool UsePointerClick;
        public int InitialPriority;
        public float SliderStep;
        [Tooltip("Optional scroll view controlled while this target is focused. Otherwise uses the nearest parent scroll view.")]
        public ScrollRect ScrollTarget;
        public bool IsFocused { get; private set; }
        private readonly Vector3[] rectCorners = new Vector3[4];
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
                if (!Focusable || !IsAvailable || !IsVisibleThroughMasks(true)) return false;
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
            if (!CanActivate) return;
            if (Control is Button button) button.onClick.Invoke();
            else if (Control is Toggle toggle) toggle.isOn = !toggle.isOn;
            else if (Control != null) ExecuteEvents.Execute(Control.gameObject, pointer, ExecuteEvents.submitHandler);
            else if (UsePointerClick)
                ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerClickHandler);
            else if (Item is UIListItemController listItem) listItem.OnClick();
            else if (EventTarget != null) ExecuteEvents.Execute(EventTarget, pointer, ExecuteEvents.pointerClickHandler);
        }

        public bool CanActivate
        {
            get
            {
                if (!CanFocus || !IsVisibleInViewport) return false;
                if (Item is IControllerSubmitAvailability availability && !availability.CanControllerSubmit) return false;
                if (Control is Slider || Control is Scrollbar) return false;
                if (Control != null) return Control is Button || Control is Toggle ||
                    Control is TMPro.TMP_InputField || ExecuteEvents.CanHandleEvent<ISubmitHandler>(Control.gameObject);
                if (UsePointerClick) return EventTarget != null && EventTarget.activeInHierarchy &&
                    ExecuteEvents.CanHandleEvent<IPointerClickHandler>(EventTarget);
                if (Item is UIListItemController) return true;
                return EventTarget != null && EventTarget.activeInHierarchy &&
                    ExecuteEvents.CanHandleEvent<IPointerClickHandler>(EventTarget);
            }
        }

        public ScrollRect GetScrollTarget() => ScrollTarget != null ? ScrollTarget : GetComponentInParent<ScrollRect>();

        public static RectTransform GetViewport(ScrollRect scroll) =>
            scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;

        public static Bounds GetRectBounds(RectTransform rect, RectTransform relativeTo, Vector3[] corners)
        {
            rect.GetWorldCorners(corners);
            var bounds = new Bounds(relativeTo.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int i = 1; i < 4; i++) bounds.Encapsulate(relativeTo.InverseTransformPoint(corners[i]));
            return bounds;
        }

        public bool IsVisibleInViewport
        {
            get
            {
                if (Rect == null || !ScreenRect.Overlaps(new Rect(0f, 0f, Screen.width, Screen.height)) ||
                    !IsVisibleThroughMasks(false)) return false;
                foreach (var scroll in Rect.GetComponentsInParent<ScrollRect>())
                {
                    if (!scroll.isActiveAndEnabled || scroll.content == null || !Rect.IsChildOf(scroll.content)) continue;
                    var viewport = GetViewport(scroll);
                    if (viewport == null) continue;
                    Bounds bounds = GetRectBounds(Rect, viewport, rectCorners);
                    Rect view = viewport.rect;
                    // Oversized targets remain usable while their center is visible.
                    if (!FitsViewport(bounds.min.x, bounds.max.x, view.xMin, view.xMax) ||
                        !FitsViewport(bounds.min.y, bounds.max.y, view.yMin, view.yMax)) return false;
                }
                return true;
            }
        }

        private bool IsVisibleThroughMasks(bool allowScrolling)
        {
            var scrolls = allowScrolling ? Rect.GetComponentsInParent<ScrollRect>() : null;
            foreach (var mask in Rect.GetComponentsInParent<RectMask2D>())
                if (mask.isActiveAndEnabled && mask.rectTransform != Rect &&
                    !FitsMask(mask.rectTransform, mask.padding, scrolls)) return false;
            foreach (var mask in Rect.GetComponentsInParent<Mask>())
                if (mask.MaskEnabled() && mask.rectTransform != Rect &&
                    !FitsMask(mask.rectTransform, Vector4.zero, scrolls)) return false;
            return true;
        }

        private bool FitsMask(RectTransform mask, Vector4 padding, ScrollRect[] scrolls)
        {
            RectTransform target = Rect;
            if (scrolls != null)
                foreach (var scroll in scrolls)
                {
                    if (!scroll.isActiveAndEnabled || scroll.content == null || !Rect.IsChildOf(scroll.content)) continue;
                    var viewport = GetViewport(scroll);
                    if (viewport == null || (viewport != mask && !viewport.IsChildOf(mask))) continue;
                    // Scrollable entries remain reachable only while their viewport is visible through outer masks.
                    target = viewport;
                }
            Bounds bounds = GetRectBounds(target, mask, rectCorners);
            Rect view = mask.rect;
            view = UnityEngine.Rect.MinMaxRect(view.xMin + padding.x, view.yMin + padding.y,
                view.xMax - padding.z, view.yMax - padding.w);
            return view.width > 0f && view.height > 0f &&
                FitsViewport(bounds.min.x, bounds.max.x, view.xMin, view.xMax) &&
                FitsViewport(bounds.min.y, bounds.max.y, view.yMin, view.yMax);
        }

        private static bool FitsViewport(float min, float max, float viewMin, float viewMax) =>
            max - min > viewMax - viewMin + 1f
                ? (min + max) * 0.5f >= viewMin && (min + max) * 0.5f <= viewMax
                : min >= viewMin - 0.5f && max <= viewMax + 0.5f;

        public void EnsureVisible()
        {
            foreach (var scroll in Rect.GetComponentsInParent<ScrollRect>())
            {
                var view = GetViewport(scroll);
                if (!scroll.isActiveAndEnabled || scroll.content == null || view == null || !Rect.IsChildOf(scroll.content)) continue;
                Bounds bounds = GetRectBounds(Rect, view, rectCorners);
                Rect viewport = view.rect;
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
                Vector3 world = view.TransformVector(offset);
                scroll.content.anchoredPosition += (Vector2)scroll.content.parent.InverseTransformVector(world);
            }
        }
    }
}
