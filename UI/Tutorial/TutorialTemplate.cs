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

    public class TutorialStage
    {

        [JsonProperty(Required = Required.Default)]
        public TutorialEntry[] Entries;

    }

    public class TutorialTemplate : BaseDataTemplate
    {

        [JsonProperty(Required = Required.Default)]
        public TutorialRect[] Rects;
        [JsonProperty(Required = Required.Default)]
        public TutorialStage[] Stages;
        [JsonProperty(Required = Required.Default)]
        public float MaxDuration = -1;
        [JsonProperty(Required = Required.Default)]
        public bool WithVeil = true;
        [JsonProperty(Required = Required.Default)]
        public bool PauseGameplay = false;

    }

}
