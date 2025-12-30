using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.VisualScripting;

namespace UnityCommonEx
{

    public class EnumMaskConverter<T> : JsonConverter<T> where T : Enum
    {

        const string Splitter = "|";

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            byte result = 0;
            if (reader.TokenType == JsonToken.String)
            {
                foreach (string e in ((string)reader.Value).Split(Splitter))
                {
                    var ee = e.Trim();
                    if (ee.Length > 0)
                    {
                        T enumValue = (T)Enum.Parse(typeof(T), ee);
                        result |= Convert.ToByte(enumValue);
                    }
                }
            }
            return (T)(object)result;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            var enumValues = new List<string>();
            var enumType = typeof(T);
            
            foreach (T enumValue in Enum.GetValues(enumType))
            {
                if (value.HasFlag(enumValue))
                {
                    enumValues.Add(enumValue.ToString());
                }
            }
            
            string result = string.Join(Splitter, enumValues);
            writer.WriteValue(result);
        }

    }

}
