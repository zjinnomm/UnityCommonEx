using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityCommonEx
{
    public class GameSettingUIController : SingletonController<GameSettingUIController>
    {
        [Header("UI Prefabs")]
        public GameObject SettingEntryItemPrefab;
        public GameObject SelectItemPrefab;
        public GameObject NumberItemPrefab;
        public GameObject ToggleItemPrefab;
        public GameObject GroupHeaderPrefab;

        [Header("UI Roots")]
        public Transform EntryRoot;

        [FormerlySerializedAs("TabRoot")]
        public Transform CategoryTabRoot;

        [FormerlySerializedAs("TabPrefab")]
        public GameObject CategoryTabPrefab;

        private GameSettingTemplate currentTemplate;
        private IGameSettingManager currentManager;
        private readonly Dictionary<string, CommonSelectableListItemUIController> categoryTabs =
            new Dictionary<string, CommonSelectableListItemUIController>();

        public bool IsGenerated => currentTemplate != null;

        public void GenerateUI<T>(GameSettingTemplate template, GameSettingManager<T> manager)
            where T : GameSetting, new()
        {
            if (template == null || manager == null)
            {
                LogUtil.Error("GameSettingUIController: Template or Manager is null");
                return;
            }

            List<GameSettingCategoryConfig> categories = template.EnumerateCategories().ToList();
            if (categories.Count == 0)
            {
                LogUtil.Warn("GameSettingUIController: Template has no categories");
                return;
            }

            currentTemplate = template;
            currentManager = manager;

            ClearUI();
            ClearTabs();

            if (EntryRoot == null)
            {
                LogUtil.Error("GameSettingUIController: EntryRoot is not configured");
                return;
            }

            if (CategoryTabRoot != null && CategoryTabPrefab != null)
            {
                for (int i = 0; i < categories.Count; i++)
                    CreateCategoryTab(categories[i]);

                ShowCategory(categories[0].CategoryName);
                return;
            }

            for (int i = 0; i < categories.Count; i++)
                CreateCategoryContent(categories[i], EntryRoot, manager);
        }

        private void ShowCategory(string categoryName)
        {
            if (currentTemplate == null || currentManager == null)
            {
                LogUtil.Error("GameSettingUIController: ShowCategory called before GenerateUI");
                return;
            }

            GameSettingCategoryConfig category = currentTemplate
                .EnumerateCategories()
                .FirstOrDefault(item => item != null && item.CategoryName == categoryName);
            if (category == null)
            {
                LogUtil.Warn("GameSettingUIController: Category '{0}' not found", categoryName);
                return;
            }

            ClearUI();
            CreateCategoryContent(category, EntryRoot, currentManager);
            RefreshTabSelection(categoryName);
        }

        private void CreateCategoryContent(
            GameSettingCategoryConfig category,
            Transform parent,
            IGameSettingManager manager)
        {
            if (category == null || parent == null || manager == null)
                return;

            if (category.Groups == null)
                return;

            for (int i = 0; i < category.Groups.Length; i++)
            {
                GameSettingGroupConfig group = category.Groups[i];
                if (group == null)
                    continue;

                CreateGroupHeader(group, parent);

                if (group.Fields == null)
                    continue;

                for (int j = 0; j < group.Fields.Length; j++)
                {
                    GameSettingFieldConfig fieldConfig = group.Fields[j];
                    if (fieldConfig == null)
                        continue;

                    CreateFieldUI(fieldConfig, parent, manager);
                }
            }
        }

        private void CreateGroupHeader(GameSettingGroupConfig group, Transform parent)
        {
            if (GroupHeaderPrefab == null || group == null || parent == null)
                return;

            SettingGroupHeaderUIController header =
                NodeController.Create<SettingGroupHeaderUIController>(GroupHeaderPrefab, parent);
            if (header == null)
            {
                LogUtil.Error("GameSettingUIController: GroupHeaderPrefab does not have SettingGroupHeaderUIController");
                return;
            }

            header.name = "Group_" + group.GroupName;
            header.SetLabel(GetDisplayText(group.DisplayName, group.GroupName));
        }

        private void CreateCategoryTab(GameSettingCategoryConfig category)
        {
            if (CategoryTabRoot == null || CategoryTabPrefab == null || category == null)
                return;

            CommonSelectableListItemUIController tab =
                NodeController.Create<CommonSelectableListItemUIController>(CategoryTabPrefab, CategoryTabRoot);
            if (tab == null)
            {
                LogUtil.Error("GameSettingUIController: CategoryTabPrefab does not have CommonSelectableListItemUIController");
                return;
            }

            tab.name = "Tab_" + category.CategoryName;
            tab.SetItem(GetDisplayText(category.DisplayName, category.CategoryName));
            tab.OnDeselected();
            if (tab.InteractiveButton != null)
                tab.InteractiveButton.onClick.AddListener(() => ShowCategory(category.CategoryName));
            categoryTabs[category.CategoryName] = tab;
        }

        private void RefreshTabSelection(string selectedCategoryName)
        {
            foreach (KeyValuePair<string, CommonSelectableListItemUIController> kv in categoryTabs)
            {
                if (kv.Value == null)
                    continue;

                if (kv.Key == selectedCategoryName)
                    kv.Value.OnSelected();
                else
                    kv.Value.OnDeselected();
            }
        }

        private void CreateFieldUI(GameSettingFieldConfig config, Transform parent, IGameSettingManager manager)
        {
            if (SettingEntryItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: SettingEntryItemPrefab is not configured");
                return;
            }

            SettingEntryItemUIController entry =
                NodeController.Create<SettingEntryItemUIController>(SettingEntryItemPrefab, parent);
            if (entry == null)
            {
                LogUtil.Error("GameSettingUIController: SettingEntryItemPrefab does not have SettingEntryItemUIController");
                return;
            }

            entry.name = "Entry_" + config.FieldName;
            entry.SetLabel(GetDisplayText(config.DisplayName, config.FieldName));

            Transform itemParent = entry.GetSettingItemRoot();

            if (config.Type == GameSettingFieldType.Select)
            {
                CreateSelectField(config, itemParent, manager);
                return;
            }

            if (config.Type == GameSettingFieldType.Number)
            {
                CreateNumberField(config, itemParent, manager);
                return;
            }

            if (config.Type == GameSettingFieldType.Toggle)
            {
                CreateToggleField(config, itemParent, manager);
                return;
            }

            LogUtil.Error("GameSettingUIController: Unsupported field type {0}", config.Type);
        }

        private void CreateSelectField(GameSettingFieldConfig config, Transform parent, IGameSettingManager manager)
        {
            if (SelectItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: SelectItemPrefab is not configured");
                return;
            }

            SelectSettingItemUIController selectItem =
                NodeController.Create<SelectSettingItemUIController>(SelectItemPrefab, parent);
            if (selectItem == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: SelectItemPrefab for field {0} does not have a SelectSettingItemUIController",
                    config.FieldName);
                return;
            }

            SelectGameSettingFieldConfig selectConfig = config as SelectGameSettingFieldConfig;
            if (selectConfig == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: Config for field {0} is not SelectGameSettingFieldConfig",
                    config.FieldName);
                return;
            }

            selectItem.name = "Select_" + config.FieldName;

            string[] options = selectConfig.Options ?? Array.Empty<string>();
            string[] displayTexts = options;
            if (selectConfig.DisplayOptions != null && selectConfig.DisplayOptions.Length == options.Length)
            {
                displayTexts = new string[options.Length];
                for (int i = 0; i < options.Length; i++)
                    displayTexts[i] = GetDisplayText(selectConfig.DisplayOptions[i], options[i]);
            }

            selectItem.SetOptions(displayTexts);

            int initialIndex = 0;
            if (options.Length > 0)
            {
                object currentValue = manager.GetValue(config.FieldName);
                if (currentValue != null)
                {
                    int found = Array.IndexOf(options, Convert.ToString(currentValue));
                    if (found >= 0)
                        initialIndex = found;
                }
            }

            selectItem.SetSelectedIndex(initialIndex);
            selectItem.OnSelectedIndexChanged += index =>
            {
                if (index < 0 || index >= options.Length)
                    return;

                manager.SetValue(config.FieldName, options[index]);
            };
        }

        private void CreateNumberField(GameSettingFieldConfig config, Transform parent, IGameSettingManager manager)
        {
            if (NumberItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: NumberItemPrefab is not configured");
                return;
            }

            NumberSettingItemUIController numberItem =
                NodeController.Create<NumberSettingItemUIController>(NumberItemPrefab, parent);
            if (numberItem == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: NumberItemPrefab for field {0} does not have a NumberSettingItemUIController",
                    config.FieldName);
                return;
            }

            NumberGameSettingFieldConfig numberConfig = config as NumberGameSettingFieldConfig;
            if (numberConfig == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: Config for field {0} is not NumberGameSettingFieldConfig",
                    config.FieldName);
                return;
            }

            numberItem.name = "Number_" + config.FieldName;
            numberItem.SetConfig(numberConfig);

            object currentValueObj = manager.GetValue(config.FieldName);
            float initialValue = numberConfig.Default;
            if (currentValueObj != null)
            {
                try
                {
                    initialValue = Convert.ToSingle(currentValueObj);
                }
                catch
                {
                    initialValue = numberConfig.Default;
                }
            }

            numberItem.SetValue(initialValue);
            numberItem.OnValueChanged += value => manager.SetValue(config.FieldName, value);
        }

        private void CreateToggleField(GameSettingFieldConfig config, Transform parent, IGameSettingManager manager)
        {
            if (ToggleItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: ToggleItemPrefab is not configured");
                return;
            }

            ToggleSettingItemUIController toggleItem =
                NodeController.Create<ToggleSettingItemUIController>(ToggleItemPrefab, parent);
            if (toggleItem == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: ToggleItemPrefab for field {0} does not have a ToggleSettingItemUIController",
                    config.FieldName);
                return;
            }

            ToggleGameSettingFieldConfig toggleConfig = config as ToggleGameSettingFieldConfig;
            if (toggleConfig == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: Config for field {0} is not ToggleGameSettingFieldConfig",
                    config.FieldName);
                return;
            }

            toggleItem.name = "Toggle_" + config.FieldName;
            toggleItem.SetConfig(toggleConfig);

            object currentValueObj = manager.GetValue(config.FieldName);
            bool initialValue = toggleConfig.Default;
            if (currentValueObj != null)
            {
                try
                {
                    initialValue = Convert.ToBoolean(currentValueObj);
                }
                catch
                {
                    initialValue = toggleConfig.Default;
                }
            }

            toggleItem.SetValue(initialValue);
            toggleItem.OnValueChanged += value => manager.SetValue(config.FieldName, value);
        }

        private void ClearUI()
        {
            if (EntryRoot == null)
                return;

            for (int i = EntryRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = EntryRoot.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        private void ClearTabs()
        {
            if (CategoryTabRoot != null)
            {
                for (int i = CategoryTabRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = CategoryTabRoot.GetChild(i);
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }

            categoryTabs.Clear();
        }

        private static string GetDisplayText(MultiLingualText text, string fallback)
        {
            string display = text?.GetText();
            return string.IsNullOrEmpty(display) ? fallback : display;
        }

        protected override void OnRelease()
        {
            ClearUI();
            ClearTabs();
            base.OnRelease();
        }
    }
}
