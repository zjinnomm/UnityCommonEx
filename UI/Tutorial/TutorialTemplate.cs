using Newtonsoft.Json;
using UnityEngine;

namespace UnityCommonEx
{

    public class TutorialRect
    {

        [JsonProperty(Required = Required.Always)]
        public Vector2 Position;
        [JsonProperty(Required = Required.Always)]
        public Vector2 Size;

        public Rect ToRect() => new Rect(Position, Size);

    }

    public class TutorialTemplate : BaseDataTemplate
    {

        [JsonProperty(Required = Required.Default)]
        public TutorialRect[] Rects;
        public TutorialEntry[] Entries;
        [JsonProperty(Required = Required.Default)]
        public float MaxDuration = -1;
        [JsonProperty(Required = Required.Default)]
        public bool WithVeil = true;
        [JsonProperty(Required = Required.Default)]
        public bool PauseGameplay = false;

    }

}
