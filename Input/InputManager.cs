using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    public abstract class InputManager<TAction> : SingletonController<InputManager<TAction>> where TAction : Enum
    {
        private sealed class ActionListener
        {
            public object Owner;
            public Action<TAction> Callback;
        }

        private static readonly IReadOnlyList<InputBinding> EmptyBindings = Array.Empty<InputBinding>();

        private readonly Dictionary<TAction, List<InputBinding>> bindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<InputBinding>> triggeredBindingsByAction = new Dictionary<TAction, List<InputBinding>>();
        private readonly Dictionary<TAction, List<ActionListener>> listenersByAction = new Dictionary<TAction, List<ActionListener>>();
        private readonly HashSet<TAction> triggeredActions = new HashSet<TAction>();
        private readonly List<TAction> actionScratch = new List<TAction>();
        private bool initialized;

        protected override bool IsPersistent => true;

        protected override void OnInit()
        {
            base.OnInit();
            DontDestroyOnLoad(this);
        }

        public void Initialize(InputBindingTemplate<TAction> template)
        {
            bindingsByAction.Clear();
            triggeredBindingsByAction.Clear();
            triggeredActions.Clear();

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
                    SetBindings(binding.Action, binding.Bindings);
                }
            }

            initialized = true;
        }

        public void SetBindings(TAction action, IEnumerable<InputBinding> bindings)
        {
            if (!bindingsByAction.TryGetValue(action, out var list))
            {
                list = new List<InputBinding>();
                bindingsByAction[action] = list;
            }

            list.Clear();
            if (bindings != null)
                list.AddRange(bindings);
        }

        public void ProcessInput()
        {
            if (!initialized)
                return;

            triggeredActions.Clear();
            foreach (var kvp in triggeredBindingsByAction)
                kvp.Value.Clear();

            actionScratch.Clear();
            foreach (var kvp in bindingsByAction)
                actionScratch.Add(kvp.Key);

            foreach (var action in actionScratch)
            {
                if (!bindingsByAction.TryGetValue(action, out var bindings) || bindings == null || bindings.Count == 0)
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
                NotifyListeners(action);
            }
        }

        public bool WasTriggeredThisFrame(TAction action)
        {
            return triggeredActions.Contains(action);
        }

        public bool IsActuated(TAction action)
        {
            if (!bindingsByAction.TryGetValue(action, out var bindings) || bindings == null)
                return false;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] != null && bindings[i].IsActuated())
                    return true;
            }

            return false;
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

        private static bool IsDeadUnityObject(object owner)
        {
            return owner is UnityEngine.Object unityObject && unityObject == null;
        }
    }
}
