using TMPro;

namespace UnityCommonEx
{
    public class SettingGroupHeaderUIController : NodeController
    {
        public TMP_Text LabelTMP;

        public void SetLabel(string text)
        {
            if (LabelTMP != null)
                LabelTMP.text = text;
        }
    }
}
