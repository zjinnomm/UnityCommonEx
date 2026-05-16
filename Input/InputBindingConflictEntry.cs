using System;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBindingConflictEntry<TAction> where TAction : struct, Enum
    {
        public TAction Action;
        public int SlotIndex;
        public InputBinding Binding;
        public bool IsLocked;
    }
}
