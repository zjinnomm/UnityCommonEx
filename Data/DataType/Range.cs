using System;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityCommonEx
{

    [Serializable]
    [JsonConverter(typeof(FloatRangeConverter))]
    public struct FloatRange
    {

        public float max;
        public float min;

        public FloatRange(float value)
        {
            min = max = value;
        }

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            return $"{min}-{max}";
        }

        public static FloatRange operator +(FloatRange a, FloatRange b) => new FloatRange(a.min + b.min, a.max + b.max);
        public static FloatRange operator *(FloatRange a, int b) => new FloatRange(a.min * b, a.max * b);
        public static FloatRange operator *(FloatRange a, float b) => new FloatRange(a.min * b, a.max * b);

        public static FloatRange FromString(string s)
        {
            if (s.Contains('-'))
            {
                string[] values = s.Split('-');
                return new FloatRange { min = float.Parse(values[0]), max = float.Parse(values[1]) };
            }
            else
            {
                float value = float.Parse(s);
                return new FloatRange { min = value, max = value};
            }
        }

        public float Value => Mathf.Lerp(min, max, UnityEngine.Random.value);

    }

    [Serializable]
    [JsonConverter(typeof(IntRangeConverter))]
    public struct IntRange
    {

        public int max;
        public int min;

        public override string ToString()
        {
            return $"{min}-{max}";
        }

        public static IntRange FromString(string s)
        {
            if (s.Contains('-'))
            {
                string[] values = s.Split('-');
                return new IntRange { min = int.Parse(values[0]), max = int.Parse(values[1]) };
            }
            else
            {
                int value = int.Parse(s);
                return new IntRange { min = value, max = value };
            }
        }

        public static IntRange operator* (IntRange range, float mult) => new IntRange { min = (int)(range.min * mult), max = (int)(range.max * mult) };

        public int Value => UnityEngine.Random.Range(min, max + 1);

    }

    public class FloatRangeConverter : JsonConverter<FloatRange>
    {

        public override FloatRange ReadJson(JsonReader reader, Type objectType, FloatRange existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return FloatRange.FromString((string)reader.Value);
            }
            else if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                float value = float.Parse(reader.Value.ToString());
                return new FloatRange { min = value, max = value };
            }
            return new FloatRange();
        }

        public override void WriteJson(JsonWriter writer, FloatRange value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.min}-{value.max}");
        }

    }

    public class IntRangeConverter : JsonConverter<IntRange>
    {

        public override IntRange ReadJson(JsonReader reader, Type objectType, IntRange existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return IntRange.FromString((string)reader.Value);
            }
            else if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                int value = int.Parse(reader.Value.ToString());
                return new IntRange { min = value, max = value };
            }
            return new IntRange();
        }

        public override void WriteJson(JsonWriter writer, IntRange value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.min}-{value.max}");
        }

    }

}