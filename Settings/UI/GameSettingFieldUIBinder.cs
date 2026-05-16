using System;
using UnityEngine;

namespace UnityCommonEx
{
    public abstract class GameSettingFieldUIBinder
    {
        public abstract GameSettingFieldType FieldType { get; }
        public abstract void Bind(GameSettingFieldConfig config, SettingEntryItemUIController entry, IGameSettingManager manager);
    }

    public abstract class GameSettingFieldUIBinder<TConfig, TItem> : GameSettingFieldUIBinder
        where TConfig : GameSettingFieldConfig
        where TItem : NodeController
    {
        private readonly GameObject prefab;
        private readonly string prefabLabel;

        protected GameSettingFieldUIBinder(GameObject prefab, string prefabLabel)
        {
            this.prefab = prefab;
            this.prefabLabel = prefabLabel;
        }

        public override void Bind(GameSettingFieldConfig config, SettingEntryItemUIController entry, IGameSettingManager manager)
        {
            if (prefab == null)
            {
                LogUtil.Error("GameSettingUIController: {0} is not configured", prefabLabel);
                return;
            }

            if (!(config is TConfig typedConfig))
            {
                LogUtil.Error(
                    "GameSettingUIController: Config for field {0} is not {1}",
                    config != null ? config.FieldName : string.Empty,
                    typeof(TConfig).Name);
                return;
            }

            TItem item = entry.CreateSettingItem<TItem>(prefab);
            if (item == null)
            {
                LogUtil.Error(
                    "GameSettingUIController: {0} for field {1} does not have a {2}",
                    prefabLabel,
                    typedConfig.FieldName,
                    typeof(TItem).Name);
                return;
            }

            item.name = GetItemName(typedConfig.FieldName);
            BindTyped(typedConfig, item, manager);
        }

        protected abstract string GetItemName(string fieldName);
        protected abstract void BindTyped(TConfig config, TItem item, IGameSettingManager manager);
    }

    public sealed class SelectGameSettingFieldUIBinder
        : GameSettingFieldUIBinder<SelectGameSettingFieldConfig, SelectSettingItemUIController>
    {
        public SelectGameSettingFieldUIBinder(GameObject prefab) : base(prefab, "SelectItemPrefab") { }

        public override GameSettingFieldType FieldType => GameSettingFieldType.Select;

        protected override string GetItemName(string fieldName) => "Select_" + fieldName;

        protected override void BindTyped(SelectGameSettingFieldConfig config, SelectSettingItemUIController item, IGameSettingManager manager)
        {
            string[] options = config.Options ?? Array.Empty<string>();
            item.SetOptions(config.GetDisplayOptionTexts());

            string currentValue = manager.GetValue(config.FieldName)?.ToString();
            item.SetSelectedIndex(config.GetSelectedIndex(currentValue));
            item.OnSelectedIndexChanged += index =>
            {
                if (index < 0 || index >= options.Length)
                    return;

                manager.SetValue(config.FieldName, options[index]);
            };
        }
    }

    public sealed class NumberGameSettingFieldUIBinder
        : GameSettingFieldUIBinder<NumberGameSettingFieldConfig, NumberSettingItemUIController>
    {
        public NumberGameSettingFieldUIBinder(GameObject prefab) : base(prefab, "NumberItemPrefab") { }

        public override GameSettingFieldType FieldType => GameSettingFieldType.Number;

        protected override string GetItemName(string fieldName) => "Number_" + fieldName;

        protected override void BindTyped(NumberGameSettingFieldConfig config, NumberSettingItemUIController item, IGameSettingManager manager)
        {
            item.SetConfig(config);
            item.SetValue(manager.GetValue(config.FieldName, config.Default));
            item.OnValueChanged += value => manager.SetValue(config.FieldName, value);
        }
    }

    public sealed class ToggleGameSettingFieldUIBinder
        : GameSettingFieldUIBinder<ToggleGameSettingFieldConfig, ToggleSettingItemUIController>
    {
        public ToggleGameSettingFieldUIBinder(GameObject prefab) : base(prefab, "ToggleItemPrefab") { }

        public override GameSettingFieldType FieldType => GameSettingFieldType.Toggle;

        protected override string GetItemName(string fieldName) => "Toggle_" + fieldName;

        protected override void BindTyped(ToggleGameSettingFieldConfig config, ToggleSettingItemUIController item, IGameSettingManager manager)
        {
            item.SetConfig(config);
            item.SetValue(manager.GetValue(config.FieldName, config.Default));
            item.OnValueChanged += value => manager.SetValue(config.FieldName, value);
        }
    }

    public sealed class InputBindingGameSettingFieldUIBinder
        : GameSettingFieldUIBinder<InputBindingGameSettingFieldConfig, InputBindingSettingItemUIController>
    {
        public InputBindingGameSettingFieldUIBinder(GameObject prefab) : base(prefab, "InputBindingItemPrefab") { }

        public override GameSettingFieldType FieldType => GameSettingFieldType.InputBinding;

        protected override string GetItemName(string fieldName) => "InputBinding_" + fieldName;

        protected override void BindTyped(InputBindingGameSettingFieldConfig config, InputBindingSettingItemUIController item, IGameSettingManager manager)
        {
            if (InputBindingSettingsRegistry.ActiveManager == null)
            {
                LogUtil.Error("GameSettingUIController: InputBindingSettingsManager is not initialized.");
                return;
            }

            item.SetConfig(config, InputBindingSettingsRegistry.ActiveManager);
        }
    }
}
