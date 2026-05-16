using System;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBindingOverrideProfile<TAction> where TAction : struct, Enum
    {
        [JsonProperty(Required = Required.Default)]
        public InputBindingOverrideEntry<TAction>[] Overrides;
    }
}
