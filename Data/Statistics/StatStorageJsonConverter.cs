using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    public sealed class StatStorageJsonConverter : JsonConverter<StatStorage>
    {
        public static void Populate(StatStorage storage, string content)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            string normalized = NormalizeJsonContent(content);
            using (var stringReader = new StringReader(normalized))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                if (!jsonReader.Read())
                    throw new JsonSerializationException("StatStorage JSON is empty.");

                var serializer = new JsonSerializer();
                var converter = new StatStorageJsonConverter();
                converter.ReadJson(jsonReader, typeof(StatStorage), storage, true, serializer);
            }
        }

        public override void WriteJson(JsonWriter writer, StatStorage value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("metrics");
            writer.WriteStartArray();
            foreach (KeyValuePair<string, Dictionary<StatPointKey, double>> metricPair in value.EnumerateMetrics())
            {
                writer.WriteStartObject();

                writer.WritePropertyName("metricId");
                writer.WriteValue(metricPair.Key);

                writer.WritePropertyName("points");
                writer.WriteStartArray();
                foreach (KeyValuePair<StatPointKey, double> pointPair in metricPair.Value)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(pointPair.Value);

                    switch (pointPair.Key.DimensionCount)
                    {
                        case 1:
                            WriteDimension(writer, pointPair.Key.Dim0);
                            break;

                        case 2:
                            WriteDimension(writer, pointPair.Key.Dim0);
                            WriteDimension(writer, pointPair.Key.Dim1);
                            break;
                    }

                    writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override StatStorage ReadJson(JsonReader reader, Type objectType, StatStorage existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (!hasExistingValue || existingValue == null)
                throw new JsonSerializationException("StatStorage must be created and initialized by the owner before deserialization.");
            if (!existingValue.IsInitialized)
                throw new JsonSerializationException("StatStorage must be initialized before deserialization.");

            List<StatSerializedMetric> metrics = null;

            ExpectToken(reader, JsonToken.StartObject);
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;
                if (reader.TokenType != JsonToken.PropertyName)
                    continue;

                string propertyName = reader.Value != null ? reader.Value.ToString() : null;
                if (!reader.Read())
                    break;

                switch (propertyName)
                {
                    case "metrics":
                        metrics = ReadMetrics(reader, serializer);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            existingValue.LoadSerializedData(metrics);
            return existingValue;
        }

        static List<StatSerializedMetric> ReadMetrics(JsonReader reader, JsonSerializer serializer)
        {
            var metrics = new List<StatSerializedMetric>();
            if (reader.TokenType == JsonToken.Null)
                return metrics;

            ExpectToken(reader, JsonToken.StartArray);
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;
                if (reader.TokenType != JsonToken.StartObject)
                    continue;

                var metric = new StatSerializedMetric();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                        break;
                    if (reader.TokenType != JsonToken.PropertyName)
                        continue;

                    string propertyName = reader.Value != null ? reader.Value.ToString() : null;
                    if (!reader.Read())
                        break;

                    switch (propertyName)
                    {
                        case "metricId":
                            metric.MetricId = reader.TokenType == JsonToken.Null ? null : reader.Value?.ToString();
                            break;

                        case "points":
                            metric.Points = ReadPoints(reader, serializer);
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }

                metrics.Add(metric);
            }

            return metrics;
        }

        static List<StatSerializedPoint> ReadPoints(JsonReader reader, JsonSerializer serializer)
        {
            var points = new List<StatSerializedPoint>();
            if (reader.TokenType == JsonToken.Null)
                return points;

            ExpectToken(reader, JsonToken.StartArray);
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;
                if (reader.TokenType != JsonToken.StartArray)
                    continue;

                var point = new StatSerializedPoint
                {
                    DimensionValues = serializer.Deserialize<object[]>(reader)
                };
                points.Add(point);
            }

            return points;
        }

        static void WriteDimension(JsonWriter writer, StatDimensionValue value)
        {
            switch (value.Kind)
            {
                case StatDimensionValueKind.Int:
                    writer.WriteValue(value.IntValue);
                    break;

                case StatDimensionValueKind.TemplateId:
                case StatDimensionValueKind.String:
                    writer.WriteValue(value.StringValue);
                    break;

                default:
                    writer.WriteNull();
                    break;
            }
        }

        static void ExpectToken(JsonReader reader, JsonToken expected)
        {
            if (reader.TokenType != expected)
                throw new JsonSerializationException($"Expected token {expected}, got {reader.TokenType}.");
        }

        static string NormalizeJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            if (content[0] == '\uFEFF')
                return content.Substring(1);
            return content;
        }
    }
}
