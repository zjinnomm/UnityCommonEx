using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityCommonEx
{

    public interface IDifferencialArrayElement { }

    public class DifferencialArrayConverter : JsonConverter
    {

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            Type elementType = objectType.GetElementType();
            JArray array = JArray.Load(reader);
            object[] result = Activator.CreateInstance(objectType, array.Count) as object[];
            for (int index = 0; index < array.Count; index++)
            {
                JToken element = array[index];
                if (element.Type == JTokenType.Null)
                {
                    continue;
                }
                JObject parseObject = element as JObject;
                JToken copySourceIndex = parseObject.ContainsKey("CopyFrom") ? parseObject["CopyFrom"] : parseObject["copyFrom"];
                if (copySourceIndex != null && copySourceIndex.Type == JTokenType.Integer)
                {
                    int indexValue = copySourceIndex.Value<int>();
                    JObject dictObject = parseObject;
                    parseObject = array[indexValue].DeepClone() as JObject;
                    foreach (var pair in dictObject)
                    {
                        if (pair.Key.ToLower() == "copyfrom")
                        {
                            continue;
                        }
                        string[] path = pair.Key.Split(".");
                        JToken replaceRoot = parseObject;
                        for (int i = 0; i < path.Length - 1; i++)
                        {
                            string key = path[i];
                            if (replaceRoot is JObject obj)
                            {
                                replaceRoot = obj[key];
                            }
                            else if (replaceRoot is JArray arr)
                            {
                                replaceRoot = arr[int.Parse(key)];
                            }
                            else
                            {
                                LogUtil.Error("parsing container is not array or object");
                            }
                        }
                        {
                            string key = path[path.Length - 1];
                            if (replaceRoot is JObject obj)
                            {
                                obj[key] = pair.Value;
                            }
                            else if (replaceRoot is JArray arr)
                            {
                                arr[int.Parse(key)] = pair.Value;
                            }
                            else
                            {
                                LogUtil.Error("parsing container is not array or object");
                            }
                        }
                    }
                }
                object instance = Activator.CreateInstance(elementType);
                serializer.Populate(parseObject.CreateReader(), instance);
                result[index] = instance;
            }
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray && typeof(IDifferencialArrayElement).IsAssignableFrom(objectType.GetElementType());
        }
    }

}