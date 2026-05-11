using System;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    [Serializable]
    public class ActionInputBinding<TAction> where TAction : Enum
    {
        [JsonProperty(Required = Required.Default)]
        public TAction Action;

        [JsonProperty(Required = Required.Default)]
        public InputBinding[] Bindings;
    }
}
