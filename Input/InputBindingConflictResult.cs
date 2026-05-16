using System;
using System.Collections.Generic;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBindingConflictResult<TAction> where TAction : struct, Enum
    {
        public readonly List<InputBindingConflictEntry<TAction>> Entries = new List<InputBindingConflictEntry<TAction>>();

        public bool HasConflict => Entries.Count > 0;

        public bool HasLockedConflict
        {
            get
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (Entries[i] != null && Entries[i].IsLocked)
                        return true;
                }

                return false;
            }
        }
    }
}
