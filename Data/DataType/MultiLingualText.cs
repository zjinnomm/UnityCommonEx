using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityCommonEx
{
    /// <summary>
    /// 多语言文本：内部为 LanguageType -> string 的映射，支持从 JSON 字符串（默认 English）或对象反序列化。
    /// </summary>
    [JsonConverter(typeof(MultiLingualTextConverter))]
    public class MultiLingualText
    {
        /// <summary>
        /// 运行时获取当前语言类型的委托，可由项目在初始化时设置（如从 HubbleBubbleSettings 读取）。
        /// </summary>
        public static Func<LanguageType> GetLanguageType
        {
            get => LanguageTypeEx.GetLanguageType;
            set => LanguageTypeEx.GetLanguageType = value;
        }

        internal Dictionary<LanguageType, string> _map = new Dictionary<LanguageType, string>();

        public MultiLingualText() { }

        public MultiLingualText(string englishText)
        {
            _map[LanguageType.English] = englishText ?? string.Empty;
        }

        /// <summary>
        /// 根据当前语言（GetLanguageType）取文案，若无则回退到 English。
        /// </summary>
        public string GetText()
        {
            var lang = LanguageTypeEx.GetCurrentLanguageType();
            if (_map.TryGetValue(lang, out var text))
                return text;
            if (_map.TryGetValue(LanguageType.English, out var fallback))
                return fallback;
            return string.Empty;
        }
    }

    public class MultiLingualTextConverter : JsonConverter<MultiLingualText>
    {
        public override MultiLingualText ReadJson(JsonReader reader, Type objectType, MultiLingualText existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var result = existingValue ?? new MultiLingualText();
            result._map = new Dictionary<LanguageType, string>();

            if (reader.TokenType == JsonToken.String)
            {
                result._map[LanguageType.English] = reader.Value?.ToString() ?? string.Empty;
                return result;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var jo = JObject.Load(reader);
                foreach (var kv in jo)
                {
                    if (string.IsNullOrEmpty(kv.Value?.ToString()))
                        continue;
                    if (Enum.TryParse<LanguageType>(kv.Key, true, out var lang))
                        result._map[lang] = kv.Value.ToString();
                }
                return result;
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, MultiLingualText value, JsonSerializer serializer)
        {
            if (value == null || value._map == null || value._map.Count == 0)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartObject();
            foreach (var kv in value._map)
            {
                writer.WritePropertyName(kv.Key.ToString());
                writer.WriteValue(kv.Value);
            }
            writer.WriteEndObject();
        }
    }
}
