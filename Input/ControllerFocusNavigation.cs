using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public sealed class ControllerFocusNavigation
    {
        private readonly List<ControllerFocusTarget> targets = new List<ControllerFocusTarget>();
        private readonly Dictionary<int, ControllerFocusTarget> memories = new Dictionary<int, ControllerFocusTarget>();
        private readonly Dictionary<(ControllerTabNavigation, int), ControllerFocusTarget> tabMemories =
            new Dictionary<(ControllerTabNavigation, int), ControllerFocusTarget>();
        private readonly List<(ControllerTabNavigation, int)> staleTabMemories = new List<(ControllerTabNavigation, int)>();
        private ControllerFocusTarget focused;
        private PointerEventData pointer;
        private EventSystem EventSystem;
        private Vector2 lastFocusPosition;
        private Vector2 lastDirection;
        private float nextRepeat;
        private int lastLayer = -1;
        private ControllerTabNavigation focusedTabs;
        private int focusedTabIndex;
        public bool NeedsContentFocusRestore => focusedTabs != null && focusedTabs.SelectedIndex != focusedTabIndex;

        public IReadOnlyList<ControllerFocusTarget> Targets => targets;
        public ControllerFocusTarget Focused => focused;
        public PointerEventData Pointer => pointer;
        public Vector2 LastFocusPosition => lastFocusPosition;
        public int Layer { get; private set; }

        public void Initialize(EventSystem eventSystem)
        {
            if (EventSystem == eventSystem && pointer != null) return;
            EventSystem = eventSystem;
            pointer = eventSystem != null ? new PointerEventData(eventSystem) { pointerId = -20 } : null;
        }

        public ControllerFocusTarget RestoreLayerTarget(int layer)
        {
            if (focused != null) memories[lastLayer] = focused;
            lastLayer = layer;
            return memories.TryGetValue(layer, out var previous) && targets.Contains(previous) ? previous : InitialTarget();
        }

        public Vector2 ReadDirection(Vector2 value, float threshold, float time,
            float initialDelay = 0.35f, float repeatInterval = 0.12f)
        {
            if (value.magnitude < threshold)
            {
                lastDirection = Vector2.zero;
                nextRepeat = 0f;
                return Vector2.zero;
            }
            Vector2 direction = Mathf.Abs(value.x) > Mathf.Abs(value.y)
                ? new Vector2(Mathf.Sign(value.x), 0f) : new Vector2(0f, Mathf.Sign(value.y));
            if (direction != lastDirection)
            {
                lastDirection = direction;
                nextRepeat = time + initialDelay;
                return direction;
            }
            if (time < nextRepeat) return Vector2.zero;
            nextRepeat = time + repeatInterval;
            return direction;
        }

        public void RefreshTargets(InputScope scope, int modalLayerThreshold)
        {
            staleTabMemories.Clear();
            foreach (var memory in tabMemories)
                if (memory.Key.Item1 == null || memory.Value == null) staleTabMemories.Add(memory.Key);
            foreach (var key in staleTabMemories) tabMemories.Remove(key);
            targets.Clear();
            Layer = 0;
            foreach (var target in ControllerFocusTarget.ActiveTargets)
            {
                if (target == null || !target.CanFocus || !IsOnScreen(target) ||
                    InputScopeRegistry.ResolveScope(target, InputScope.UI) != scope) continue;
                targets.Add(target);
                Layer = Mathf.Max(Layer, target.Layer);
            }
            if (Layer >= modalLayerThreshold) targets.RemoveAll(target => target.Layer != Layer);
            // A nested prefab belongs to its outer interactive object while embedded in a shop or list entry.
            targets.RemoveAll(target => HasAvailableAncestor(target));
        }

        private static bool IsOnScreen(ControllerFocusTarget target)
        {
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            if (target.ScreenRect.Overlaps(screen)) return true;
            var scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll?.viewport == null) return false;
            var canvas = scroll.viewport.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            scroll.viewport.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y).Overlaps(screen);
        }

        private bool HasAvailableAncestor(ControllerFocusTarget target)
        {
            for (var parent = target.transform.parent; parent != null; parent = parent.parent)
            {
                var ancestor = parent.GetComponent<ControllerFocusTarget>();
                if (ancestor != null && (ancestor.Item != null || ancestor.Control != null) &&
                    ancestor.CanFocus && targets.Contains(ancestor)) return true;
            }
            return false;
        }

        public ControllerFocusTarget InitialTarget()
        {
            ControllerFocusTarget result = null;
            foreach (var candidate in targets)
                if (result == null || candidate.InitialPriority > result.InitialPriority ||
                    (candidate.InitialPriority == result.InitialPriority &&
                     candidate.ScreenPosition.y - candidate.ScreenPosition.x * 0.01f >
                     result.ScreenPosition.y - result.ScreenPosition.x * 0.01f)) result = candidate;
            return result;
        }

        public ControllerFocusTarget NearestTarget(Vector2 position)
        {
            ControllerFocusTarget result = null;
            float distance = float.MaxValue;
            foreach (var candidate in targets)
            {
                float next = (candidate.ScreenPosition - position).sqrMagnitude;
                if (next >= distance) continue;
                distance = next;
                result = candidate;
            }
            return result;
        }

        public ControllerFocusTarget RestoreContentTarget()
        {
            var tabs = focusedTabs;
            if (tabs != null && tabs.isActiveAndEnabled)
            {
                if (tabMemories.TryGetValue((tabs, tabs.SelectedIndex), out var previous) && targets.Contains(previous))
                    return previous;
                foreach (var target in targets)
                    if (tabs.ContentRoot != null && target.transform.IsChildOf(tabs.ContentRoot)) return target;
            }
            return NearestTarget(lastFocusPosition) ?? InitialTarget();
        }

        public void SetFocus(ControllerFocusTarget target)
        {
            if (pointer == null) return;
            focusedTabs = target != null ? target.GetComponentInParent<ControllerTabNavigation>() : null;
            focusedTabIndex = focusedTabs != null ? focusedTabs.SelectedIndex : 0;
            if (focusedTabs != null && focusedTabs.ContentRoot != null && target.transform.IsChildOf(focusedTabs.ContentRoot))
                tabMemories[(focusedTabs, focusedTabIndex)] = target;
            if (focused == target) return;
            if (focused != null && pointer != null) focused.Focus(false, pointer);
            focused = target;
            if (focused == null) { EventSystem.SetSelectedGameObject(null); return; }
            focused.EnsureVisible();
            lastFocusPosition = focused.ScreenPosition;
            pointer.position = lastFocusPosition;
            focused.Focus(true, pointer);
            EventSystem.SetSelectedGameObject(focused.Control != null && !(focused.Control is TMP_InputField)
                ? focused.Control.gameObject : null);
        }

        public void MoveFocus(Vector2 direction)
        {
            if (focused == null) { SetFocus(InitialTarget()); return; }
            if (focused.Control is Slider slider && direction.x != 0f)
            {
                slider.value += direction.x * (focused.SliderStep > 0f ? focused.SliderStep :
                    slider.wholeNumbers ? 1f : Mathf.Max((slider.maxValue - slider.minValue) / 100f, 0.01f));
                return;
            }
            var origin = focused.ScreenPosition;
            ControllerFocusTarget best = null;
            float bestScore = float.MaxValue;
            foreach (var candidate in targets)
            {
                if (candidate == focused) continue;
                Vector2 delta = candidate.ScreenPosition - origin;
                float forward = Vector2.Dot(delta, direction);
                if (forward < 1f) continue;
                float sideways = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float score = forward + sideways * 3f + sideways * sideways / Mathf.Max(forward, 1f);
                if (candidate.RegionRoot != focused.RegionRoot) score += 100f;
                if (score >= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            if (best != null) SetFocus(best);
        }

        public void ChangeRegion(int delta)
        {
            var regions = new List<Transform>();
            foreach (var target in targets)
                if (!regions.Contains(target.RegionRoot)) regions.Add(target.RegionRoot);
            if (regions.Count < 2)
            {
                if (targets.Count == 0) return;
                int index = Mathf.Max(0, targets.IndexOf(focused));
                SetFocus(targets[(index + delta + targets.Count) % targets.Count]);
                return;
            }
            int current = regions.IndexOf(focused != null ? focused.RegionRoot : null);
            var region = regions[(Mathf.Max(0, current) + delta + regions.Count) % regions.Count];
            foreach (var target in targets)
                if (target.RegionRoot == region) { SetFocus(target); return; }
        }

        public void ScrollFocused(Vector2 value, float speed, float deltaTime)
        {
            var scroll = focused != null ? focused.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            float distance = scroll.content.rect.height - scroll.viewport.rect.height;
            if (scroll.vertical && distance > 0f)
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition +
                    value.y * speed * deltaTime / distance);
            float width = scroll.content.rect.width - scroll.viewport.rect.width;
            if (scroll.horizontal && width > 0f)
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition +
                    value.x * speed * deltaTime / width);
        }
    }
}
