using Newtonsoft.Json;

namespace UnityCommonEx
{

    public class InputTutorialEntry : TutorialEntry
    {

        [JsonProperty(Required = Required.Default)]
        public bool DeactivatePrevious = true;

        public override void OnActivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.AddInputRegion(record.Index, GetRegion(record.RectOverride));
        }

        public override void OnUpdate(float delta, ref TutorialManager.ActivatedEntry record)
        {
            if (InteractionModel.IsPressed && GetRegion(record.RectOverride).Contains(InteractionModel.Pos))
            {
                if (DeactivatePrevious)
                {
                    TutorialManager.Instance.DeactivateAll();
                }
                else
                {
                    TutorialManager.Instance.Deactivate(record.Index);
                }
            }
        }

        public override void OnDeactivate(ref TutorialManager.ActivatedEntry record)
        {
            TutorialUIController.Instance.RemoveObject(record.Index);
        }

    }

}