using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    public enum InputScope
    {
        World = 0,
        UI = 1,
        FullScreenUI = 2,
        Dialog = 3,
        Exclusive = 4,
    }

    public static class InputScopeRegistry
    {
        private sealed class ActiveUIScope
        {
            public object Owner;
            public Transform Root;
            public InputScope Scope;
        }

        private static readonly List<ActiveUIScope> ActiveUIScopes = new List<ActiveUIScope>();

        public static InputScope CurrentScope
        {
            get
            {
                RemoveInvalidEntries();

                InputScope result = InputScope.World;
                for (int i = 0; i < ActiveUIScopes.Count; i++)
                {
                    ActiveUIScope entry = ActiveUIScopes[i];
                    if (IsEntryActive(entry) && entry.Scope > result)
                        result = entry.Scope;
                }
                return result;
            }
        }

        public static void ActivateUI(object owner, Transform root, InputScope scope)
        {
            if (owner == null || root == null)
                return;

            for (int i = 0; i < ActiveUIScopes.Count; i++)
            {
                ActiveUIScope entry = ActiveUIScopes[i];
                if (!ReferenceEquals(entry.Owner, owner))
                    continue;

                entry.Root = root;
                entry.Scope = scope;
                return;
            }

            ActiveUIScopes.Add(new ActiveUIScope
            {
                Owner = owner,
                Root = root,
                Scope = scope,
            });
        }

        public static void DeactivateUI(object owner)
        {
            if (owner == null)
                return;

            for (int i = ActiveUIScopes.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(ActiveUIScopes[i].Owner, owner))
                    ActiveUIScopes.RemoveAt(i);
            }
        }

        public static InputScope ResolveScope(object owner, InputScope declaredScope)
        {
            RemoveInvalidEntries();

            Transform ownerTransform = GetOwnerTransform(owner);
            if (ownerTransform == null)
                return declaredScope;

            InputScope result = declaredScope;
            for (int i = 0; i < ActiveUIScopes.Count; i++)
            {
                ActiveUIScope entry = ActiveUIScopes[i];
                if (IsEntryActive(entry) && entry.Scope > result && ownerTransform.IsChildOf(entry.Root))
                    result = entry.Scope;
            }
            return result;
        }

        private static Transform GetOwnerTransform(object owner)
        {
            if (owner is Component component && component != null)
                return component.transform;
            if (owner is GameObject gameObject && gameObject != null)
                return gameObject.transform;
            return null;
        }

        private static void RemoveInvalidEntries()
        {
            for (int i = ActiveUIScopes.Count - 1; i >= 0; i--)
            {
                ActiveUIScope entry = ActiveUIScopes[i];
                if (entry == null || entry.Root == null || IsDeadUnityObject(entry.Owner))
                    ActiveUIScopes.RemoveAt(i);
            }
        }

        private static bool IsEntryActive(ActiveUIScope entry)
        {
            return entry != null && entry.Root != null && entry.Root.gameObject.activeInHierarchy;
        }

        private static bool IsDeadUnityObject(object owner)
        {
            return owner is Object unityObject && unityObject == null;
        }
    }
}
