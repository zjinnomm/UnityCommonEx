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
    }

    public class SelectGameSettingFieldConfig : GameSettingFieldConfig
    {
        [JsonProperty(Required = Required.Default)]
        public string[] Options;

        [JsonProperty(Required = Required.Default)]
        public MultiLingualText[] DisplayOptions;
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
