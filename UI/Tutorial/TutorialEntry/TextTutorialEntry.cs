using UnityEngine;

namespace UnityCommonEx
{

    public class TextTutorialEntry : TutorialEntry
    {
        
        public string Text;

        public override void OnActivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.AddText(record.Index, GetRegion(record.RectOverride), Text);
        }

        public override void OnDeactivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.RemoveObject(record.Index);
        }

    }

}