using Newtonsoft.Json;
using UnityEngine;

namespace UnityCommonEx
{

    public class TextTutorialEntry : TutorialEntry
    {

        public MultiLingualText Text;
        [JsonProperty(Required = Required.Default)]
        public bool AutoPlace = true;

        public override void OnActivate(ref TutorialManager.ActivatedEntry record)
        {
            if (AutoPlace)
            {
                TutorialUIController.Instance.AddTextNearRegion(record.Index, record.Rect, Text?.GetText() ?? string.Empty);
            }
            else
            {
                TutorialUIController.Instance.AddText(record.Index, record.Rect, Text?.GetText() ?? string.Empty);
            }
        }

        public override void OnDeactivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.RemoveObject(record.Index);
        }

    }

}
