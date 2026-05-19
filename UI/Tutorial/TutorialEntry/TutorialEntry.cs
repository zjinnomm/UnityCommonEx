using UnityEngine;
using Newtonsoft.Json;

namespace UnityCommonEx
{

    public enum TutorialEntryType
    {
        Highlight,
        Text,
    }

    [JsonConverter(typeof(TypeReflectableDataConverter<TutorialEntry, TutorialEntryType>))]
    [DataInspector.PolymorphicInstance]
    public abstract class TutorialEntry : TypeReflectableData<TutorialEntry, TutorialEntryType>
    {

        public static void RegisterTypeReflections()
        {
            RegisterTypeReflection(typeof(HighlightTutorialEntry), TutorialEntryType.Highlight);
            RegisterTypeReflection(typeof(TextTutorialEntry), TutorialEntryType.Text);
        }

        [JsonProperty(Required = Required.Default)]
        public float Delay = 0;
        [JsonProperty(Required = Required.Default)]
        public float Duration = -1;
        [JsonProperty(Required = Required.Default)]
        public int RectIndex = 0;
        [JsonProperty(Required = Required.Default)]
        public bool CanSkip = true;
        [JsonProperty(Required = Required.Default)]
        public bool Blocking = false;

        [JsonIgnore]
        public virtual bool IsBlocking => Blocking;

        public virtual void OnActivate(ref TutorialManager.ActivatedEntry record) { }
        public virtual void OnUpdate(float delta, ref TutorialManager.ActivatedEntry record) { }
        public virtual void OnDeactivate(ref TutorialManager.ActivatedEntry record) { }

    }

}
