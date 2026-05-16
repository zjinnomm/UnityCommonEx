using System;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBindingOverrideEntry<TAction> where TAction : struct, Enum
    {
        [JsonProperty(Required = Required.Default)]
        public TAction Action;

        [JsonProperty(Required = Required.Default)]
        public int SlotIndex;

        [JsonProperty(Required = Required.Default)]
        public InputBinding Binding;
    }
}
