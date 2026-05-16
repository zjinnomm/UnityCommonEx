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
        private readonly Dictionary<GameSettingFieldType, GameSettingFieldUIBinder> fieldBinders =
            new Dictionary<GameSettingFieldType, GameSettingFieldUIBinder>();

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
            header.SetLabel(GameSettingFieldConfig.GetDisplayText(group.DisplayName, group.GroupName));
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
            tab.SetItem(GameSettingFieldConfig.GetDisplayText(category.DisplayName, category.CategoryName));
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
            entry.SetLabel(config.DisplayName, config.FieldName);

            GameSettingFieldUIBinder binder = GetFieldBinder(config.Type);
            if (binder != null)
            {
                binder.Bind(config, entry, manager);
                return;
            }

            LogUtil.Error("GameSettingUIController: Unsupported field type {0}", config.Type);
        }

        private GameSettingFieldUIBinder GetFieldBinder(GameSettingFieldType fieldType)
        {
            if (fieldBinders.TryGetValue(fieldType, out GameSettingFieldUIBinder binder))
                return binder;

            binder = CreateFieldBinder(fieldType);
            if (binder != null)
                fieldBinders[fieldType] = binder;

            return binder;
        }

        private GameSettingFieldUIBinder CreateFieldBinder(GameSettingFieldType fieldType)
        {
            switch (fieldType)
            {
                case GameSettingFieldType.Select:
                    return new SelectGameSettingFieldUIBinder(SelectItemPrefab);
                case GameSettingFieldType.Number:
                    return new NumberGameSettingFieldUIBinder(NumberItemPrefab);
                case GameSettingFieldType.Toggle:
                    return new ToggleGameSettingFieldUIBinder(ToggleItemPrefab);
                default:
                    return null;
            }
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
        protected override void OnRelease()
        {
            ClearUI();
            ClearTabs();
            base.OnRelease();
        }
    }
}
