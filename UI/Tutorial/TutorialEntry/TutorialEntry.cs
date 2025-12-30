using UnityEngine;
using Newtonsoft.Json;

namespace UnityCommonEx
{

    public enum TutorialEntryType
    {
        Highlight,
        Text,
        Input,
    }

    [JsonConverter(typeof(TypeReflectableDataConverter<TutorialEntry, TutorialEntryType>))]
    [DataInspector.PolymorphicInstance]
    public abstract class TutorialEntry : TypeReflectableData<TutorialEntry, TutorialEntryType>
    {

        public static void RegisterTypeReflections()
        {
            RegisterTypeReflection(typeof(HighlightTutorialEntry), TutorialEntryType.Highlight);
            RegisterTypeReflection(typeof(TextTutorialEntry), TutorialEntryType.Text);
            RegisterTypeReflection(typeof(InputTutorialEntry), TutorialEntryType.Input);
        }

        [JsonProperty(Required = Required.Default)]
        public float Delay = 0;
        [JsonProperty(Required = Required.Default)]
        public float Duration = -1;
        [JsonProperty(Required = Required.Default)]
        public Vector2 Position = Vector2.zero;
        [JsonProperty(Required = Required.Default)]
        public Vector2 Size = Vector2.zero;

        [JsonIgnore]
        public virtual bool IsBlocking => false;

        protected Rect GetRegion(Rect rectOverride) => new Rect(rectOverride.min + Position, rectOverride.size + Size);

        public virtual void OnActivate(ref TutorialManager.ActivatedEntry record) { }
        public virtual void OnUpdate(float delta, ref TutorialManager.ActivatedEntry record) { }
        public virtual void OnDeactivate(ref TutorialManager.ActivatedEntry record) { }

    }

}