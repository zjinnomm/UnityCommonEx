using System;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    public abstract class InputBindingTemplate<TAction> : BaseDataTemplate where TAction : struct, Enum
    {
        [JsonProperty(Required = Required.Default)]
        public ActionInputBinding<TAction>[] ActionBindings;

        public override string Validate()
        {
            if (ActionBindings == null || ActionBindings.Length == 0)
                return "ActionBindings is empty";

            return null;
        }
    }
}
