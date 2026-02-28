using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置UI控制器，根据模板自动生成分页UI
    /// </summary>
    public class GameSettingUIController : SingletonController<GameSettingUIController>
    {

        /// <summary>
        /// 单条设置项容器预制体：
        /// - 根节点挂载 <see cref="SettingEntryItemUIController"/>
        /// - 负责在一行内水平布局 Label 与具体 SettingItem 控件
        /// </summary>
        [Header("UI预制体引用")]
        public GameObject SettingEntryItemPrefab;

        /// <summary>
        /// Select 类型字段的 UI 预制体：
        /// - 根节点需挂载 <see cref="SelectSettingItemUIController"/>（例如 <see cref="SpinnerSelectSettingItemUIController"/>）
        /// - 具体视觉和交互逻辑由该 Controller 实现
        /// </summary>
        public GameObject SelectItemPrefab;

        /// <summary>
        /// 字段条目容器：所有 Label + SelectItem 都会实例化为其子节点
        /// </summary>
        [Header("UI容器")]
        public Transform EntryRoot;

        /// <summary>
        /// Tab 容器（可选）：如果配置，则按 Group 生成 Tab，并通过点击 Tab 切换 EntryRoot 的内容。
        /// 如果不配置 TabRoot，则所有字段按顺序直接平铺在 EntryRoot 下。
        /// </summary>
        public Transform TabRoot;

        /// <summary>
        /// Tab 预制体（可选）：
        /// - 根节点：带 RectTransform
        /// - 挂载 Button（用于点击切换 Group）
        /// - 挂载 TMP_Text 或 Text（用于显示 GroupName 或本地化后的标题）
        /// </summary>
        public GameObject TabPrefab;

        // 内部数据
        private GameSettingTemplate _currentTemplate;
        private IGameSettingManager _currentManager;
        private readonly Dictionary<string, GameObject> _tabs = new Dictionary<string, GameObject>();

        /// <summary>
        /// 当前是否已根据模板生成过 UI（用于语言切换后判断是否需要重载）
        /// </summary>
        public bool IsGenerated => _currentTemplate != null;

        /// <summary>
        /// 根据模板生成UI
        /// </summary>
        /// <typeparam name="T">设置类型</typeparam>
        /// <param name="template">设置模板</param>
        /// <param name="manager">设置管理器</param>
        public void GenerateUI<T>(GameSettingTemplate template, GameSettingManager<T> manager) where T : GameSetting, new()
        {
            if (template == null || manager == null)
            {
                LogUtil.Error("GameSettingUIController: Template or Manager is null");
                return;
            }

            if (template.Fields == null || template.Fields.Length == 0)
            {
                LogUtil.Warn("GameSettingUIController: Template has no fields");
                return;
            }

            _currentTemplate = template;
            _currentManager = manager;

            // 清理现有UI与 Tabs
            ClearUI();
            ClearTabs();

            if (EntryRoot == null)
            {
                LogUtil.Error("GameSettingUIController: EntryRoot is not configured");
                return;
            }

            // 分组排序
            var grouped = template.Fields
                .GroupBy(f => f.GroupName)
                .OrderBy(g => g.First().GroupOrder)
                .ThenBy(g => g.Key)
                .ToList();

            // 如果配置了 TabRoot + TabPrefab，则按 Group 创建 Tab，并通过 Tab 切换内容
            if (TabRoot != null && TabPrefab != null)
            {
                foreach (var group in grouped)
                {
                    string groupName = group.Key;
                    CreateTab(groupName);
                }

                // 默认显示第一个分组
                if (grouped.Count > 0)
                {
                    ShowGroup(grouped[0].Key);
                }
            }
            else
            {
                // 未配置 Tab，则所有字段直接平铺在 EntryRoot 下
                var orderedFields = grouped
                    .SelectMany(g => g.OrderBy(f => f.FieldOrder));

                foreach (var fieldConfig in orderedFields)
                {
                    CreateFieldUI(fieldConfig, EntryRoot, manager as IGameSettingManager);
                }
            }
        }

        /// <summary>
        /// 创建字段 UI（目前仅支持 Select 类型）
        /// </summary>
        private void CreateFieldUI(GameSettingFieldConfig config, Transform parent, IGameSettingManager manager)
        {
            if (config.Type != GameSettingFieldType.Select)
            {
                LogUtil.Error("GameSettingUIController: Unsupported field type {0}", config.Type);
                return;
            }

            if (SettingEntryItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: SettingEntryItemPrefab is not configured");
                return;
            }

            if (SelectItemPrefab == null)
            {
                LogUtil.Error("GameSettingUIController: SelectItemPrefab is not configured");
                return;
            }

            // 1. 创建整行 Entry 容器
            var entry = NodeController.Create<SettingEntryItemUIController>(SettingEntryItemPrefab, parent);
            if (entry == null)
            {
                LogUtil.Error("GameSettingUIController: SettingEntryItemPrefab does not have SettingEntryItemUIController");
                return;
            }
            entry.name = $"Entry_{config.FieldName}";
            entry.SetLabel(config.DisplayName != null ? config.DisplayName.GetText() : config.FieldName);

            // 2. 创建 Select 控件到 Entry 的 SettingItemRoot 下
            var selectParent = entry.GetSettingItemRoot();

            var selectItem = NodeController.Create<SelectSettingItemUIController>(SelectItemPrefab, selectParent);
            if (selectItem == null)
            {
                LogUtil.Error("GameSettingUIController: SelectItemPrefab for field {0} does not have a SelectSettingItemUIController", config.FieldName);
                return;
            }
            selectItem.name = $"Select_{config.FieldName}";

            // 配置选项
            var selectConfig = config as SelectGameSettingFieldConfig;
            if (selectConfig == null)
            {
                LogUtil.Error("GameSettingUIController: Config for field {0} is not SelectGameSettingFieldConfig", config.FieldName);
                return;
            }

            var options = selectConfig.Options ?? Array.Empty<string>();
            string[] displayTexts = options;
            if (selectConfig.DisplayOptions != null && selectConfig.DisplayOptions.Length == options.Length)
            {
                displayTexts = new string[options.Length];
                for (int i = 0; i < options.Length; i++)
                {
                    displayTexts[i] = selectConfig.DisplayOptions[i]?.GetText() ?? options[i];
                }
            }
            selectItem.SetOptions(displayTexts);

            // 根据当前 GameSetting 值计算初始索引
            int initialIndex = 0;
            if (options.Length > 0)
            {
                object currentValue = manager.GetValue(config.FieldName);
                if (currentValue != null)
                {
                    string currentStr = Convert.ToString(currentValue);
                    int found = Array.IndexOf(options, currentStr);
                    if (found >= 0)
                    {
                        initialIndex = found;
                    }
                }
            }
            selectItem.SetSelectedIndex(initialIndex);

            // 3. 订阅变更事件，写回 GameSetting
            selectItem.OnSelectedIndexChanged += index =>
            {
                if (index < 0 || index >= options.Length)
                {
                    return;
                }

                string selectedStr = options[index];
                // 直接写入字符串，底层 GameSettingManager 会根据字段类型做 Convert.ChangeType
                manager.SetValue(config.FieldName, selectedStr);
            };
        }

        /// <summary>
        /// 根据 GroupName 切换当前显示的字段（仅在启用 Tab 模式时使用）
        /// </summary>
        /// <param name="groupName">要显示的分组名称</param>
        private void ShowGroup(string groupName)
        {
            if (_currentTemplate == null || _currentManager == null)
            {
                LogUtil.Error("GameSettingUIController: ShowGroup called before GenerateUI");
                return;
            }

            if (EntryRoot == null)
            {
                LogUtil.Error("GameSettingUIController: EntryRoot is not configured");
                return;
            }

            // 清空当前 Entry
            ClearUI();

            // 找到目标分组并按 FieldOrder 排序生成
            var fieldsInGroup = _currentTemplate.Fields
                .Where(f => f.GroupName == groupName)
                .OrderBy(f => f.FieldOrder);

            foreach (var fieldConfig in fieldsInGroup)
            {
                CreateFieldUI(fieldConfig, EntryRoot, _currentManager);
            }

            // 更新 Tab 高亮
            foreach (var kv in _tabs)
            {
                var tabGO = kv.Value;
                if (tabGO == null) continue;

                var img = tabGO.GetComponent<Image>();
                if (img != null)
                {
                    img.color = kv.Key == groupName
                        ? new Color(1f, 1f, 1f)
                        : new Color(0.8f, 0.8f, 0.8f);
                }
            }
        }

        /// <summary>
        /// 创建一个 Tab 并绑定点击事件
        /// </summary>
        private void CreateTab(string groupName)
        {
            if (TabRoot == null || TabPrefab == null)
            {
                return;
            }

            var tab = GameObject.Instantiate(TabPrefab, TabRoot);
            tab.name = $"Tab_{groupName}";
            _tabs[groupName] = tab;

            // 设置显示文本（如果有）
            var tmpText = tab.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = groupName;
            }
            else
            {
                var uiText = tab.GetComponentInChildren<UnityEngine.UI.Text>();
                if (uiText != null)
                {
                    uiText.text = groupName;
                }
            }

            var button = tab.GetComponent<Button>();
            if (button != null)
            {
                string captured = groupName;
                button.onClick.AddListener(() => ShowGroup(captured));
            }
        }

        /// <summary>
        /// 清理 EntryRoot 中的所有字段条目
        /// </summary>
        private void ClearUI()
        {
            if (EntryRoot == null)
            {
                return;
            }

            for (int i = EntryRoot.childCount - 1; i >= 0; i--)
            {
                var child = EntryRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// 清理所有 Tabs（如果有）
        /// </summary>
        private void ClearTabs()
        {
            if (TabRoot != null)
            {
                for (int i = TabRoot.childCount - 1; i >= 0; i--)
                {
                    var child = TabRoot.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            _tabs.Clear();
        }

        protected override void OnRelease()
        {
            ClearUI();
            base.OnRelease();
        }

    }

}