using System;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBindingSetResult
    {
        public bool Success;
        public bool ClearedExistingConflict;
        public string ActionName;
        public int SlotIndex;
        public InputBinding Binding;
        public string Message;
    }
}
