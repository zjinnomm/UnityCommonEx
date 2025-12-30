using Newtonsoft.Json;

namespace UnityCommonEx
{

    public class TutorialTemplate : BaseDataTemplate
    {

        public TutorialEntry[] Entries;
        [JsonProperty(Required = Required.Default)]
        public float MaxDuration = -1;
        [JsonProperty(Required = Required.Default)]
        public bool WithVeil = true;

    }

}