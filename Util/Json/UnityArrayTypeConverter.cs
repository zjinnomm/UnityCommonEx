using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityCommonEx
{

    public class UnityArrayTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2) || objectType == typeof(Vector3) || objectType == typeof(Vector2Int) || objectType == typeof(Vector3Int)  || objectType == typeof(Color) || objectType == typeof(Color32);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                LogUtil.Error("UnityArrayTypeConverter: ReadJson: Not a start array");
                return null;
            }
            JArray array = JArray.Load(reader);
            if (objectType == typeof(Vector2))
            {
                return new Vector2((float)array[0], (float)array[1]);
            }
            if (objectType == typeof(Vector3))
            {
                return new Vector3((float)array[0], (float)array[1], (float)array[2]);
            }
            if (objectType == typeof(Vector2Int))
            {
                return new Vector2Int((int)array[0], (int)array[1]);
            }
            if (objectType == typeof(Vector3Int))
            {
                return new Vector3Int((int)array[0], (int)array[1], (int)array[2]);
            }
            if (objectType == typeof(Color))
            {
                return new Color((float)array[0], (float)array[1], (float)array[2], array.Count > 3 ? (float)array[3] : 1);
            }
            if (objectType == typeof(Color32))
            {
                return new Color32((byte)array[0], (byte)array[1], (byte)array[2], array.Count > 3 ? (byte)array[3] : byte.MaxValue);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            if (value is Vector2 vector2)
            {
                writer.WriteValue(vector2.x);
                writer.WriteValue(vector2.y);
            }
            else if (value is Vector3 vector3)
            {
                writer.WriteValue(vector3.x);
                writer.WriteValue(vector3.y);
                writer.WriteValue(vector3.z);
            }
            else if (value is Vector2Int vector2int)
            {
                writer.WriteValue(vector2int.x);
                writer.WriteValue(vector2int.y);
            }
            else if (value is Vector3Int vector3int)
            {
                writer.WriteValue(vector3int.x);
                writer.WriteValue(vector3int.y);
                writer.WriteValue(vector3int.z);
            }
            else if (value is Color color)
            {
                writer.WriteValue(color.r);
                writer.WriteValue(color.g);
                writer.WriteValue(color.b);
                writer.WriteValue(color.a);
            }
            else if (value is Color32 color32)
            {
                writer.WriteValue(color32.r);
                writer.WriteValue(color32.g);
                writer.WriteValue(color32.b);
                writer.WriteValue(color32.a);
            }
            writer.WriteEndArray();
        }
    }

}