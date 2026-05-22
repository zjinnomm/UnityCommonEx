using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    public abstract class InputManager<TAction> : IInputBindingSettingsManager where TAction : struct, Enum
    {
        private sealed class ActionListener
        {
            public object Owner;
            public Action<TAction> Callback;
        }

        private sealed class RebindState
        {
            public TAction Action;
            public string ActionName;
            public int SlotIndex;
            public InputBindingTriggerType TriggerType;
            public bool Rebindable = true;
            public int StartedFrame;
        }

        private static readonly IReadOnlyList<InputBinding> EmptyBindings = Array.Empty<InputBinding>();
        private static InputManager<TAction> _instance;
        private static readonly KeyCode[] CapturableKeys = BuildCapturableKeys();

        private readonly Dictionary<TAction, List<InputBinding>> defaultBindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<InputBinding>> committedBindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<InputBinding>> pendingBindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<InputBinding>> triggeredBindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<ActionListener>> listenersByAction = new Dictionary<TAction, List<ActionListener>>();
        private readonly Dictionary<TAction, List<IInputActionWrapper<TAction>>> wrappersByAction = new Dictionary<TAction, List<IInputActionWrapper<TAction>>>();
        private readonly HashSet<TAction> triggeredActions = new HashSet<TAction>();
        private readonly List<TAction> actionScratch = new List<TAction>();
        private string overridePath;
        private bool initialized;
        private RebindState activeRebind;
        private int displayRevision;

        public static InputManager<TAction> Instance => _instance;
        public bool HasPendingChanges => !AreBindingSetsEqual(pendingBindingsByAction, committedBindingsByAction);
        public bool IsRebinding => activeRebind != null;
        public int DisplayRevision => displayRevision;

        protected InputManager()
        {
            _instance = this;
        }

        private void MarkDisplayDirty()
        {
            displayRevision++;
        }

        public void Initialize(InputBindingTemplate<TAction> template, string inputOverridePath = null)
        {
            defaultBindingsByAction.Clear();
            committedBindingsByAction.Clear();
            pendingBindingsByAction.Clear();
            triggeredBindingsByAction.Clear();
            triggeredActions.Clear();
            overridePath = inputOverridePath;
            activeRebind = null;
            InputBindingSettingsRegistry.ActiveManager = this;
            displayRevision = 0;

            if (template == null)
            {
                initialized = false;
                LogUtil.Error("{0} initialize failed: input binding template is null.", GetType().Name);
                return;
            }

            if (template.ActionBindings != null)
            {
                for (int i = 0; i < template.ActionBindings.Length; i++)
                {
                    ActionInputBinding<TAction> binding = template.ActionBindings[i];
                    if (binding == null)
                        continue;
                    List<InputBinding> defaultBindings = CloneBindings(binding.Bindings);
                    defaultBindingsByAction[binding.Action] = defaultBindings;
                    committedBindingsByAction[binding.Action] = CloneBindings(defaultBindings);
                    pendingBindingsByAction[binding.Action] = CloneBindings(defaultBindings);
                }
            }

            LoadOverrides();
            MarkDisplayDirty();
            initialized = true;
        }

        public void SetBindings(TAction action, IEnumerable<InputBinding> bindings)
        {
            List<InputBinding> defaultBindings = CloneBindings(bindings);
            defaultBindingsByAction[action] = CloneBindings(defaultBindings);
            committedBindingsByAction[action] = CloneBindings(defaultBindings);
            pendingBindingsByAction[action] = CloneBindings(defaultBindings);
            MarkDisplayDirty();
        }

        public IReadOnlyList<InputBinding> GetPendingBindings(string actionName)
        {
            if (!TryParseAction(actionName, out TAction action))
                return EmptyBindings;

            return GetPendingBindings(action);
        }

        public IReadOnlyList<InputBinding> GetPendingBindings(TAction action)
        {
            if (pendingBindingsByAction.TryGetValue(action, out var bindings) && bindings != null && bindings.Count > 0)
                return bindings;
            return EmptyBindings;
        }

        public InputBinding GetPendingBinding(TAction action, int slotIndex)
        {
            if (!pendingBindingsByAction.TryGetValue(action, out var bindings))
                return null;
            if (slotIndex < 0 || slotIndex >= bindings.Count)
                return null;
            return bindings[slotIndex];
        }

        public void ResetPendingBindings(string actionName)
        {
            if (TryParseAction(actionName, out TAction action))
                ResetPendingBindings(action);
        }

        public void ResetPendingBindings(TAction action)
        {
            if (defaultBindingsByAction.TryGetValue(action, out var bindings))
                pendingBindingsByAction[action] = CloneBindings(bindings);
            else
                pendingBindingsByAction[action] = new List<InputBinding>();
            MarkDisplayDirty();
        }

        public void ResetAllPendingBindings()
        {
            activeRebind = null;
            foreach (var kv in defaultBindingsByAction)
                pendingBindingsByAction[kv.Key] = CloneBindings(kv.Value);
            MarkDisplayDirty();
        }

        public void ApplyChanges()
        {
            foreach (var kv in pendingBindingsByAction)
                committedBindingsByAction[kv.Key] = CloneBindings(kv.Value);

            SaveOverrides();
            MarkDisplayDirty();
        }

        public void RevertChanges()
        {
            activeRebind = null;
            foreach (var kv in committedBindingsByAction)
                pendingBindingsByAction[kv.Key] = CloneBindings(kv.Value);
            MarkDisplayDirty();
        }

        public InputBindingSetResult TrySetPendingBinding(string actionName, int slotIndex, InputBinding binding)
        {
            if (!TryParseAction(actionName, out TAction action))
            {
                return new InputBindingSetResult
                {
                    Success = false,
                    ActionName = actionName,
                    SlotIndex = slotIndex,
                    Binding = binding?.Clone(),
                    Message = $"Unknown action '{actionName}'."
                };
            }

            return TrySetPendingBinding(action, slotIndex, binding);
        }

        public InputBindingSetResult ClearPendingBinding(string actionName, int slotIndex)
        {
            if (!TryParseAction(actionName, out TAction action))
            {
                return new InputBindingSetResult
                {
                    Success = false,
                    ActionName = actionName,
                    SlotIndex = slotIndex,
                    Message = $"Unknown action '{actionName}'."
                };
            }

            return ClearPendingBinding(action, slotIndex);
        }

        public bool BeginRebind(string actionName, int slotIndex)
        {
            if (!TryParseAction(actionName, out TAction action))
                return false;

            return BeginRebind(action, slotIndex);
        }

        public bool BeginRebind(TAction action, int slotIndex)
        {
            List<InputBinding> bindings = GetOrCreatePendingBindingList(action);
            if (slotIndex < 0)
                return false;

            EnsureSlot(bindings, slotIndex);
            InputBinding binding = bindings[slotIndex];
            if (binding != null && !binding.Rebindable)
                return false;

            InputBinding defaultBinding = GetDefaultBinding(action, slotIndex);
            if (binding == null && defaultBinding != null && !defaultBinding.Rebindable)
                return false;

            activeRebind = new RebindState
            {
                Action = action,
                ActionName = action.ToString(),
                SlotIndex = slotIndex,
                TriggerType = binding != null ? binding.TriggerType :
                    (defaultBinding != null ? defaultBinding.TriggerType : InputBindingTriggerType.Pressed),
                Rebindable = binding != null ? binding.Rebindable : (defaultBinding == null || defaultBinding.Rebindable),
                StartedFrame = Time.frameCount
            };

            MarkDisplayDirty();
            return true;
        }

        public void ProcessInput()
        {
            if (!initialized || activeRebind != null)
                return;

            triggeredActions.Clear();
            foreach (var kvp in triggeredBindingsByAction)
                kvp.Value.Clear();

            actionScratch.Clear();
            foreach (var kvp in committedBindingsByAction)
                actionScratch.Add(kvp.Key);

            foreach (var action in actionScratch)
            {
                if (!committedBindingsByAction.TryGetValue(action, out var bindings) || bindings == null || bindings.Count == 0)
                    continue;

                List<InputBinding> triggeredBindings = GetOrCreateTriggeredBindingList(action);
                triggeredBindings.Clear();

                for (int i = 0; i < bindings.Count; i++)
                {
                    InputBinding binding = bindings[i];
                    if (binding != null && binding.IsTriggeredThisFrame())
                        triggeredBindings.Add(binding);
                }

                if (triggeredBindings.Count == 0)
                    continue;

                triggeredActions.Add(action);
                if (TryTriggerTopWrapper(action))
                    continue;
                NotifyListeners(action);
            }
        }

        public bool WasTriggeredThisFrame(TAction action)
        {
            return triggeredActions.Contains(action);
        }

        public bool IsActuated(TAction action)
        {
            if (!committedBindingsByAction.TryGetValue(action, out var bindings) || bindings == null)
                return false;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] != null && bindings[i].IsActuated())
                    return true;
            }

            return false;
        }

        public bool IsRebindingTarget(string actionName, int slotIndex)
        {
            return activeRebind != null &&
                activeRebind.SlotIndex == slotIndex &&
                string.Equals(activeRebind.ActionName, actionName, StringComparison.Ordinal);
        }

        public bool TryFinishActiveRebindThisFrame(out InputBindingSetResult result)
        {
            result = null;
            if (activeRebind == null)
                return false;

            bool canConsumeMouseControl = Time.frameCount > activeRebind.StartedFrame;

            if (canConsumeMouseControl && Input.GetMouseButtonDown(0))
            {
                result = new InputBindingSetResult
                {
                    Success = false,
                    ActionName = activeRebind.ActionName,
                    SlotIndex = activeRebind.SlotIndex,
                    Message = "Rebind cancelled."
                };
                activeRebind = null;
                MarkDisplayDirty();
                return true;
            }

            if (canConsumeMouseControl && Input.GetMouseButtonDown(1))
            {
                result = ClearPendingBinding(activeRebind.Action, activeRebind.SlotIndex);
                activeRebind = null;
                MarkDisplayDirty();
                return true;
            }

            if (!TryCaptureBinding(activeRebind, out InputBinding binding))
                return false;

            result = TrySetPendingBinding(activeRebind.Action, activeRebind.SlotIndex, binding);
            activeRebind = null;
            MarkDisplayDirty();
            return true;
        }

        public void CancelRebind()
        {
            activeRebind = null;
            MarkDisplayDirty();
        }

        public IReadOnlyList<InputBinding> GetTriggeredBindings(TAction action)
        {
            if (triggeredBindingsByAction.TryGetValue(action, out var bindings) && bindings != null && bindings.Count > 0)
                return bindings;
            return EmptyBindings;
        }

        public bool WasTriggeredBy(TAction action, Predicate<InputBinding> predicate)
        {
            if (predicate == null)
                return false;

            IReadOnlyList<InputBinding> bindings = GetTriggeredBindings(action);
            for (int i = 0; i < bindings.Count; i++)
            {
                if (predicate((InputBinding)bindings[i]))
                    return true;
            }

            return false;
        }

        public void RegisterListener(TAction action, object owner, Action<TAction> callback)
        {
            if (callback == null)
                return;

            if (!listenersByAction.TryGetValue(action, out var listeners))
            {
                listeners = new List<ActionListener>();
                listenersByAction[action] = listeners;
            }

            for (int i = 0; i < listeners.Count; i++)
            {
                ActionListener listener = listeners[i];
                if (ReferenceEquals(listener.Owner, owner) && listener.Callback == callback)
                    return;
            }

            listeners.Add(new ActionListener
            {
                Owner = owner,
                Callback = callback
            });
        }

        public void RegisterWrapper(IInputActionWrapper<TAction> wrapper)
        {
            if (wrapper == null)
                return;

            TAction action = wrapper.Action;
            if (!wrappersByAction.TryGetValue(action, out var wrappers))
            {
                wrappers = new List<IInputActionWrapper<TAction>>();
                wrappersByAction[action] = wrappers;
            }

            for (int i = 0; i < wrappers.Count; i++)
            {
                if (ReferenceEquals(wrappers[i], wrapper))
                    return;
            }

            wrappers.Add(wrapper);
        }

        public void UnregisterWrapper(IInputActionWrapper<TAction> wrapper)
        {
            if (wrapper == null)
                return;

            TAction action = wrapper.Action;
            if (!wrappersByAction.TryGetValue(action, out var wrappers))
                return;

            for (int i = wrappers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(wrappers[i], wrapper))
                    wrappers.RemoveAt(i);
            }
        }

        public void UnregisterListener(TAction action, object owner)
        {
            if (!listenersByAction.TryGetValue(action, out var listeners))
                return;

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(listeners[i].Owner, owner))
                    listeners.RemoveAt(i);
            }
        }

        public void UnregisterAllListeners(object owner)
        {
            if (owner == null)
                return;

            foreach (var kvp in listenersByAction)
            {
                List<ActionListener> listeners = kvp.Value;
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(listeners[i].Owner, owner))
                        listeners.RemoveAt(i);
                }
            }
        }

        private List<InputBinding> GetOrCreateTriggeredBindingList(TAction action)
        {
            if (!triggeredBindingsByAction.TryGetValue(action, out var bindings))
            {
                bindings = new List<InputBinding>();
                triggeredBindingsByAction[action] = bindings;
            }

            return bindings;
        }

        private List<InputBinding> GetOrCreatePendingBindingList(TAction action)
        {
            if (!pendingBindingsByAction.TryGetValue(action, out var bindings))
            {
                bindings = new List<InputBinding>();
                pendingBindingsByAction[action] = bindings;
            }

            return bindings;
        }

        private InputBindingSetResult TrySetPendingBinding(TAction action, int slotIndex, InputBinding binding)
        {
            var result = new InputBindingSetResult
            {
                Success = false,
                ActionName = action.ToString(),
                SlotIndex = slotIndex,
                Binding = binding?.Clone()
            };

            if (slotIndex < 0)
            {
                result.Message = "Slot index cannot be negative.";
                return result;
            }

            List<InputBinding> bindings = GetOrCreatePendingBindingList(action);
            EnsureSlot(bindings, slotIndex);

            InputBinding currentBinding = bindings[slotIndex];
            InputBinding defaultBinding = GetDefaultBinding(action, slotIndex);
            if (currentBinding != null && !currentBinding.Rebindable)
            {
                result.Message = $"Binding '{action}' slot {slotIndex} is locked.";
                return result;
            }
            if (currentBinding == null && defaultBinding != null && !defaultBinding.Rebindable)
            {
                result.Message = $"Binding '{action}' slot {slotIndex} is locked.";
                return result;
            }

            if (binding == null)
                return ClearPendingBinding(action, slotIndex);

            var conflicts = FindConflicts(action, slotIndex, binding);
            if (conflicts.HasLockedConflict)
            {
                result.Message = $"Binding '{binding.GetDisplayText()}' is already used by a locked action.";
                return result;
            }

            if (conflicts.HasConflict)
            {
                for (int i = 0; i < conflicts.Entries.Count; i++)
                {
                    InputBindingConflictEntry<TAction> entry = conflicts.Entries[i];
                    if (entry == null)
                        continue;

                    List<InputBinding> conflictBindings = GetOrCreatePendingBindingList(entry.Action);
                    EnsureSlot(conflictBindings, entry.SlotIndex);
                    conflictBindings[entry.SlotIndex] = null;
                }

                result.ClearedExistingConflict = true;
            }

            binding.Rebindable = currentBinding != null ? currentBinding.Rebindable : (defaultBinding == null || defaultBinding.Rebindable);
            bindings[slotIndex] = binding.Clone();
            MarkDisplayDirty();
            result.Success = true;
            result.Message = result.ClearedExistingConflict ? "Rebound and cleared previous conflicting binding." : "Rebound successfully.";
            return result;
        }

        private InputBindingSetResult ClearPendingBinding(TAction action, int slotIndex)
        {
            var result = new InputBindingSetResult
            {
                ActionName = action.ToString(),
                SlotIndex = slotIndex
            };

            if (slotIndex < 0)
            {
                result.Success = false;
                result.Message = "Slot index cannot be negative.";
                return result;
            }

            List<InputBinding> bindings = GetOrCreatePendingBindingList(action);
            EnsureSlot(bindings, slotIndex);

            InputBinding currentBinding = bindings[slotIndex];
            InputBinding defaultBinding = GetDefaultBinding(action, slotIndex);
            if (currentBinding != null && !currentBinding.Rebindable)
            {
                result.Success = false;
                result.Message = $"Binding '{action}' slot {slotIndex} is locked.";
                return result;
            }
            if (currentBinding == null && defaultBinding != null && !defaultBinding.Rebindable)
            {
                result.Success = false;
                result.Message = $"Binding '{action}' slot {slotIndex} is locked.";
                return result;
            }

            bindings[slotIndex] = null;
            MarkDisplayDirty();
            result.Success = true;
            result.Message = "Binding cleared.";
            return result;
        }

        private InputBindingConflictResult<TAction> FindConflicts(TAction action, int slotIndex, InputBinding binding)
        {
            var result = new InputBindingConflictResult<TAction>();
            if (binding == null)
                return result;

            foreach (var kv in pendingBindingsByAction)
            {
                List<InputBinding> bindings = kv.Value;
                if (bindings == null)
                    continue;

                for (int i = 0; i < bindings.Count; i++)
                {
                    if (kv.Key.Equals(action) && i == slotIndex)
                        continue;

                    InputBinding existingBinding = bindings[i];
                    if (existingBinding == null)
                        continue;

                    if (!existingBinding.MatchesPhysicalInput(binding))
                        continue;

                    result.Entries.Add(new InputBindingConflictEntry<TAction>
                    {
                        Action = kv.Key,
                        SlotIndex = i,
                        Binding = existingBinding.Clone(),
                        IsLocked = !existingBinding.Rebindable
                    });
                }
            }

            return result;
        }

        private void LoadOverrides()
        {
            if (string.IsNullOrEmpty(overridePath) || !File.Exists(overridePath))
                return;

            try
            {
                InputBindingOverrideProfile<TAction> profile =
                    JsonUtil.Read<InputBindingOverrideProfile<TAction>>(overridePath, JsonReadFailureMode.Warning);
                if (profile?.Overrides == null)
                    return;

                for (int i = 0; i < profile.Overrides.Length; i++)
                {
                    InputBindingOverrideEntry<TAction> entry = profile.Overrides[i];
                    if (entry == null || entry.SlotIndex < 0)
                        continue;

                    List<InputBinding> committedBindings = GetOrCreateBindingList(committedBindingsByAction, entry.Action);
                    List<InputBinding> pendingBindings = GetOrCreateBindingList(pendingBindingsByAction, entry.Action);
                    EnsureSlot(committedBindings, entry.SlotIndex);
                    EnsureSlot(pendingBindings, entry.SlotIndex);
                    committedBindings[entry.SlotIndex] = entry.Binding?.Clone();
                    pendingBindings[entry.SlotIndex] = entry.Binding?.Clone();
                }

                MarkDisplayDirty();
            }
            catch (Exception ex)
            {
                LogUtil.Error("{0} failed to load input overrides: {1}", GetType().Name, ex.Message);
            }
        }

        private void SaveOverrides()
        {
            if (string.IsNullOrEmpty(overridePath))
                return;

            var overrides = new List<InputBindingOverrideEntry<TAction>>();
            var actions = new HashSet<TAction>(defaultBindingsByAction.Keys);
            foreach (var action in committedBindingsByAction.Keys)
                actions.Add(action);

            foreach (TAction action in actions)
            {
                defaultBindingsByAction.TryGetValue(action, out var defaultBindings);
                committedBindingsByAction.TryGetValue(action, out var committedBindings);

                int count = Math.Max(defaultBindings?.Count ?? 0, committedBindings?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    InputBinding defaultBinding = GetBindingAt(defaultBindings, i);
                    InputBinding committedBinding = GetBindingAt(committedBindings, i);
                    if (AreBindingsEqual(defaultBinding, committedBinding))
                        continue;

                    overrides.Add(new InputBindingOverrideEntry<TAction>
                    {
                        Action = action,
                        SlotIndex = i,
                        Binding = committedBinding?.Clone()
                    });
                }
            }

            JsonUtil.Write(overridePath, new InputBindingOverrideProfile<TAction>
            {
                Overrides = overrides.ToArray()
            });
        }

        private InputBinding GetDefaultBinding(TAction action, int slotIndex)
        {
            if (!defaultBindingsByAction.TryGetValue(action, out var bindings))
                return null;
            return GetBindingAt(bindings, slotIndex);
        }

        private static InputBinding GetBindingAt(List<InputBinding> bindings, int index)
        {
            if (bindings == null || index < 0 || index >= bindings.Count)
                return null;
            return bindings[index];
        }

        private static List<InputBinding> CloneBindings(IEnumerable<InputBinding> bindings)
        {
            var list = new List<InputBinding>();
            if (bindings == null)
                return list;

            foreach (InputBinding binding in bindings)
                list.Add(binding?.Clone());

            return list;
        }

        private static void EnsureSlot(List<InputBinding> bindings, int slotIndex)
        {
            while (bindings.Count <= slotIndex)
                bindings.Add(null);
        }

        private static bool AreBindingsEqual(InputBinding left, InputBinding right)
        {
            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;
            return left.MatchesExact(right);
        }

        private static bool AreBindingSetsEqual(
            Dictionary<TAction, List<InputBinding>> left,
            Dictionary<TAction, List<InputBinding>> right)
        {
            var actions = new HashSet<TAction>(left.Keys);
            foreach (var action in right.Keys)
                actions.Add(action);

            foreach (TAction action in actions)
            {
                left.TryGetValue(action, out var leftBindings);
                right.TryGetValue(action, out var rightBindings);
                int count = Math.Max(leftBindings?.Count ?? 0, rightBindings?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    if (!AreBindingsEqual(GetBindingAt(leftBindings, i), GetBindingAt(rightBindings, i)))
                        return false;
                }
            }

            return true;
        }

        private static List<InputBinding> GetOrCreateBindingList(
            Dictionary<TAction, List<InputBinding>> source,
            TAction action)
        {
            if (!source.TryGetValue(action, out var bindings))
            {
                bindings = new List<InputBinding>();
                source[action] = bindings;
            }

            return bindings;
        }

        private static KeyCode[] BuildCapturableKeys()
        {
            Array values = Enum.GetValues(typeof(KeyCode));
            var keys = new List<KeyCode>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                KeyCode key = (KeyCode)values.GetValue(i);
                if (key != KeyCode.None)
                    keys.Add(key);
            }

            return keys.ToArray();
        }

        private bool TryCaptureBinding(RebindState state, out InputBinding binding)
        {
            binding = null;
            if (state == null)
                return false;

            for (int i = 0; i < CapturableKeys.Length; i++)
            {
                KeyCode key = CapturableKeys[i];
                if (!Input.GetKeyDown(key))
                    continue;

                binding = InputBinding.Keyboard(key, state.TriggerType);
                binding.Rebindable = state.Rebindable;
                return true;
            }

            return false;
        }

        private static bool TryParseAction(string actionName, out TAction action)
        {
            if (!string.IsNullOrEmpty(actionName) && Enum.TryParse(actionName, out action))
                return true;

            action = default;
            return false;
        }

        private void NotifyListeners(TAction action)
        {
            if (!listenersByAction.TryGetValue(action, out var listeners) || listeners == null || listeners.Count == 0)
                return;

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                ActionListener listener = listeners[i];
                if (listener == null || listener.Callback == null || IsDeadUnityObject(listener.Owner))
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                listener.Callback(action);
            }
        }

        private bool TryTriggerTopWrapper(TAction action)
        {
            if (!wrappersByAction.TryGetValue(action, out var wrappers) || wrappers == null || wrappers.Count == 0)
                return false;

            for (int i = wrappers.Count - 1; i >= 0; i--)
            {
                IInputActionWrapper<TAction> wrapper = wrappers[i];
                if (wrapper == null || IsDeadUnityObject(wrapper))
                {
                    wrappers.RemoveAt(i);
                    continue;
                }

                if (!wrapper.CanTrigger())
                    continue;

                wrapper.Trigger();
                return true;
            }

            return false;
        }

        private static bool IsDeadUnityObject(object owner)
        {
            return owner is UnityEngine.Object unityObject && unityObject == null;
        }
    }
}
