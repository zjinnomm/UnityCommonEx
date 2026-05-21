using Newtonsoft.Json;

namespace UnityCommonEx
{

    public class HighlightTutorialEntry : TutorialEntry
    {

        public enum ShapeType : byte
        {
            Box,
            Circle,
        }

        [JsonProperty(Required = Required.Default)]
        public ShapeType Shape = ShapeType.Box;

        public override void OnActivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.AddVeilMask(record.Key, record.Rect, Shape);
        }

        public override void OnDeactivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.RemoveObject(record.Key);
        }

    }

}
