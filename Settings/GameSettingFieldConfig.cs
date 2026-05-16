using System;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    [JsonConverter(typeof(TypeReflectableDataTemplateConverter<GameSettingFieldConfig, GameSettingFieldType>))]
    public abstract class GameSettingFieldConfig : TypeReflectableDataTemplate<GameSettingFieldConfig, GameSettingFieldType>
    {
        public string FieldName;
        public MultiLingualText DisplayName;

        [JsonProperty(Required = Required.Default)]
        public CachedStaticMethodRef OnChangedFunc;

        [JsonProperty(Required = Required.Default)]
        public bool ChangeImmediately = false;

        public static void RegisterTypeReflections()
        {
            RegisterTypeReflection(typeof(SelectGameSettingFieldConfig), GameSettingFieldType.Select);
            RegisterTypeReflection(typeof(NumberGameSettingFieldConfig), GameSettingFieldType.Number);
            RegisterTypeReflection(typeof(ToggleGameSettingFieldConfig), GameSettingFieldType.Toggle);
        }

        public string GetDisplayNameText()
        {
            return GetDisplayText(DisplayName, FieldName);
        }

        public static string GetDisplayText(MultiLingualText text, string fallback)
        {
            string display = text?.GetText();
            return string.IsNullOrEmpty(display) ? fallback : display;
        }
    }

    public class SelectGameSettingFieldConfig : GameSettingFieldConfig
    {
        [JsonProperty(Required = Required.Default)]
        public string[] Options;

        [JsonProperty(Required = Required.Default)]
        public MultiLingualText[] DisplayOptions;

        public string[] GetDisplayOptionTexts()
        {
            string[] options = Options ?? Array.Empty<string>();
            string[] displayTexts = options;
            if (DisplayOptions != null && DisplayOptions.Length == options.Length)
            {
                displayTexts = new string[options.Length];
                for (int i = 0; i < options.Length; i++)
                    displayTexts[i] = GetDisplayText(DisplayOptions[i], options[i]);
            }

            return displayTexts;
        }

        public int GetSelectedIndex(string currentValue)
        {
            string[] options = Options ?? Array.Empty<string>();
            if (options.Length == 0 || string.IsNullOrEmpty(currentValue))
                return 0;

            int index = Array.IndexOf(options, currentValue);
            return index >= 0 ? index : 0;
        }
    }

    public class NumberGameSettingFieldConfig : GameSettingFieldConfig
    {
        [JsonProperty(Required = Required.Default)]
        public float Min = 0f;

        [JsonProperty(Required = Required.Default)]
        public float Max = 100f;

        [JsonProperty(Required = Required.Default)]
        public float Step = 1f;

        [JsonProperty(Required = Required.Default)]
        public float Default = 100f;
    }

    public class ToggleGameSettingFieldConfig : GameSettingFieldConfig
    {
        [JsonProperty(Required = Required.Default)]
        public bool Default = false;
    }
}
