using Newtonsoft.Json;

namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置字段的配置信息（TypeReflected 基类），用于定义 UI 显示和编辑规则。
    /// 
    /// 通过 <see cref="GameSettingFieldType"/> + TypeReflected 机制，将不同字段类型
    /// 映射为不同的派生配置类，例如：
    /// - <see cref="SelectGameSettingFieldConfig"/> 对应 <see cref="GameSettingFieldType.Select"/>
    /// </summary>
    [JsonConverter(typeof(TypeReflectableDataTemplateConverter<GameSettingFieldConfig, GameSettingFieldType>))]
    public abstract class GameSettingFieldConfig : TypeReflectableDataTemplate<GameSettingFieldConfig, GameSettingFieldType>
    {
        /// <summary>
        /// 对应GameSetting中的字段名（必须与字段名完全匹配）
        /// </summary>
        public string FieldName;

        /// <summary>
        /// UI 显示名称（多语言）
        /// </summary>
        public MultiLingualText DisplayName;

        /// <summary>
        /// 分组名称（用于分页，相同GroupName的字段会在同一页）
        /// </summary>
        public string GroupName;

        /// <summary>
        /// 分组排序（数字越小越靠前）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public int GroupOrder = 0;

        /// <summary>
        /// 字段在组内排序（数字越小越靠前）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public int FieldOrder = 0;

        /// <summary>
        /// 字段值改变或首次加载完配置后调用的静态方法（格式 ClassName.MethodName，签名 void Method(object value)）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public CachedStaticMethodRef OnChangedFunc;

        /// <summary>
        /// 注册各字段类型与派生配置类的映射关系。
        /// </summary>
        public static void RegisterTypeReflections()
        {
            RegisterTypeReflection(typeof(SelectGameSettingFieldConfig), GameSettingFieldType.Select);
            RegisterTypeReflection(typeof(NumberGameSettingFieldConfig), GameSettingFieldType.Number);
        }

    }

    /// <summary>
    /// Select 类型字段的配置：从若干字符串选项中选择其一。
    /// </summary>
    public class SelectGameSettingFieldConfig : GameSettingFieldConfig
    {
        /// <summary>
        /// 选项值列表（用于 Select 类型，必填）：写回 GameSetting 的原始字符串值，底层通过 Convert.ChangeType 转成字段类型。
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string[] Options;

        /// <summary>
        /// 选项的显示文案（多语言），与 Options 一一对应；若为 null 或长度不一致则用 Options 作为显示。
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public MultiLingualText[] DisplayOptions;

    }

    /// <summary>
    /// Number 类型字段的配置：在 Min-Max 范围内按 Step 调整数值。
    /// </summary>
    public class NumberGameSettingFieldConfig : GameSettingFieldConfig
    {
        /// <summary>
        /// 最小值（默认 0）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public float Min = 0f;

        /// <summary>
        /// 最大值（默认 100）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public float Max = 100f;

        /// <summary>
        /// 步进（默认 1）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public float Step = 1f;

        /// <summary>
        /// 默认值（默认 100）
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public float Default = 100f;
    }

}