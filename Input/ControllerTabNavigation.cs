using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    [DisallowMultipleComponent]
    public sealed class ControllerTabNavigation : MonoBehaviour
    {
        public Button[] Tabs;
        public Transform ContentRoot;
        public int SelectedIndex { get; private set; }

        public void SetTabs(IList<Button> tabs)
        {
            Tabs = new Button[tabs.Count];
            tabs.CopyTo(Tabs, 0);
            SelectedIndex = 0;
        }

        public void SetSelectedIndex(int index) => SelectedIndex = index;
        public void Previous() => Change(-1);
        public void Next() => Change(1);

        private void Change(int delta)
        {
            if (Tabs == null || Tabs.Length < 2) return;
            for (int step = 1; step < Tabs.Length; step++)
            {
                int index = ((SelectedIndex + delta * step) % Tabs.Length + Tabs.Length) % Tabs.Length;
                var tab = Tabs[index];
                if (tab == null || !tab.IsActive() || !tab.IsInteractable()) continue;
                tab.onClick.Invoke();
                return;
            }
        }
    }
}
