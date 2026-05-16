using TMPro;
using UnityEngine;

namespace UnityCommonEx
{
    public class SettingEntryItemUIController : NodeController
    {
        [Header("UI References")]
        public TMP_Text LabelTMP;
        public Transform SettingItemRoot;

        public void SetLabel(string displayName)
        {
            if (LabelTMP != null)
                LabelTMP.text = displayName;
        }

        public void SetLabel(MultiLingualText displayName, string fallback)
        {
            SetLabel(GameSettingFieldConfig.GetDisplayText(displayName, fallback));
        }

        public Transform GetSettingItemRoot()
        {
            return SettingItemRoot != null ? SettingItemRoot : transform;
        }

        public T CreateSettingItem<T>(GameObject prefab) where T : NodeController
        {
            if (prefab == null)
                return null;

            return NodeController.Create<T>(prefab, GetSettingItemRoot());
        }
    }
}
