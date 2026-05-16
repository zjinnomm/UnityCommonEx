using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    [CreateAssetMenu(fileName = "InputBindingIconResourceConfig", menuName = "UnityCommonEx/Input Binding Icon Resource")]
    public class InputBindingIconResourceConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string BindingKey;
            public Sprite Sprite;
            public string FallbackText;
        }

        public List<Entry> Entries = new List<Entry>();

        private Dictionary<string, Entry> _cache;

        public Entry GetEntry(string bindingKey)
        {
            if (_cache == null)
                Initialize();

            if (!string.IsNullOrEmpty(bindingKey) && _cache.TryGetValue(bindingKey, out Entry entry))
                return entry;

            return null;
        }

        public void Initialize()
        {
            _cache = new Dictionary<string, Entry>();
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry entry = Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.BindingKey))
                    continue;

                _cache[entry.BindingKey] = entry;
            }
        }
    }
}
