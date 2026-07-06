using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    [JsonConverter(typeof(StatStorageJsonConverter))]
    public sealed class StatStorage
    {
        readonly Dictionary<string, Dictionary<StatPointKey, double>> values =
            new Dictionary<string, Dictionary<StatPointKey, double>>(StringComparer.OrdinalIgnoreCase);

        StatSchemaSet schema;

        public bool IsDirty { get; private set; }
        public bool IsInitialized => schema != null;
        [JsonIgnore]
        public bool Merged { get; private set; }
        [JsonIgnore]
        public StatSchemaSet Schema => schema;

        public void Init(string schemaTemplate)
        {
            string resolvedTemplate = StatisticSchemaManager.Instance.ResolveTemplate(schemaTemplate);
            Init(StatisticSchemaManager.Instance.Get(resolvedTemplate));
        }

        public void Init(StatSchemaSet schemaSet)
        {
            if (schemaSet == null)
                throw new ArgumentNullException(nameof(schemaSet));

            schema = schemaSet;
            Merged = false;
            Normalize();
        }

        public void Clear()
        {
            values.Clear();
            IsDirty = false;
            Merged = false;
        }

        public void Record(string metricId, double value = 1d)
        {
            RecordInternal(metricId, value, 0, default(StatDimensionValue), default(StatDimensionValue));
        }

        public void Record(string metricId, double value, in StatDimensionValue dim0)
        {
            RecordInternal(metricId, value, 1, dim0, default(StatDimensionValue));
        }

        public void Record(string metricId, double value, in StatDimensionValue dim0, in StatDimensionValue dim1)
        {
            RecordInternal(metricId, value, 2, dim0, dim1);
        }

        public double Read(string metricId)
        {
            return TryRead(metricId, out double value) ? value : 0d;
        }

        public double Read(string metricId, in StatDimensionValue dim0)
        {
            return TryRead(metricId, out double value, dim0) ? value : 0d;
        }

        public double Read(string metricId, in StatDimensionValue dim0, in StatDimensionValue dim1)
        {
            return TryRead(metricId, out double value, dim0, dim1) ? value : 0d;
        }

        public bool TryRead(string metricId, out double value)
        {
            return TryReadInternal(metricId, out value, 0, default(StatDimensionValue), default(StatDimensionValue));
        }

        public bool TryRead(string metricId, out double value, in StatDimensionValue dim0)
        {
            return TryReadInternal(metricId, out value, 1, dim0, default(StatDimensionValue));
        }

        public bool TryRead(string metricId, out double value, in StatDimensionValue dim0, in StatDimensionValue dim1)
        {
            return TryReadInternal(metricId, out value, 2, dim0, dim1);
        }

        public void CombineTo(StatStorage target)
        {
            if (!IsInitialized)
            {
                LogUtil.Error("[StatStorage] CombineTo source is not initialized.");
                return;
            }
            if (Merged)
            {
                LogUtil.Error("[StatStorage] CombineTo source has already been merged.");
                return;
            }
            if (target == null)
            {
                LogUtil.Error("[StatStorage] CombineTo target is null.");
                return;
            }
            if (!target.IsInitialized)
            {
                LogUtil.Error("[StatStorage] CombineTo target is not initialized.");
                return;
            }
            if (target.Merged)
            {
                LogUtil.Error("[StatStorage] CombineTo target has already been merged.");
                return;
            }
            if (!HasSameSchema(target))
            {
                LogUtil.Error("[StatStorage] CombineTo schema mismatch.");
                return;
            }

            foreach (KeyValuePair<string, Dictionary<StatPointKey, double>> metricPair in values)
            {
                if (!target.TryGetMetricDefinition(metricPair.Key, out StatMetricDefinition definition))
                    continue;

                foreach (KeyValuePair<StatPointKey, double> pointPair in metricPair.Value)
                    target.ApplyAggregatedValue(metricPair.Key, definition, pointPair.Key, pointPair.Value);
            }

            Merged = true;
        }

        public void Normalize()
        {
            if (schema == null)
                return;

            var rebuilt = new Dictionary<string, Dictionary<StatPointKey, double>>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, Dictionary<StatPointKey, double>> metricPair in values)
            {
                if (!TryGetMetricDefinition(metricPair.Key, out StatMetricDefinition definition))
                    continue;

                foreach (KeyValuePair<StatPointKey, double> pointPair in metricPair.Value)
                {
                    if (!TryNormalizeStoredPoint(definition, pointPair.Key, pointPair.Value, out StatPointKey normalizedKey, out double normalizedValue))
                        continue;

                    ApplyAggregatedValue(metricPair.Key, definition, normalizedKey, normalizedValue, rebuilt, false);
                }
            }

            values.Clear();
            foreach (KeyValuePair<string, Dictionary<StatPointKey, double>> metricPair in rebuilt)
                values.Add(metricPair.Key, metricPair.Value);
        }

        public string SaveRaw()
        {
            return JsonUtil.WriteRaw(this);
        }

        public void Save(string path)
        {
            JsonUtil.Write(path, this);
        }

        public void LoadRaw(string content)
        {
            StatStorageJsonConverter.Populate(this, content);
        }

        public void Load(string path, JsonReadFailureMode failureMode = JsonReadFailureMode.Warning)
        {
            try
            {
                LoadRaw(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                if (failureMode == JsonReadFailureMode.Warning)
                {
                    LogUtil.Warn("StatStorage.Load failed at '{0}': {1}", path, ex.Message);
                    return;
                }

                LogUtil.Error("StatStorage.Load failed at '{0}': {1}", path, ex.Message);
            }
        }

        internal IEnumerable<KeyValuePair<string, Dictionary<StatPointKey, double>>> EnumerateMetrics()
        {
            return values;
        }

        internal void LoadSerializedData(IEnumerable<StatSerializedMetric> metrics)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("StatStorage must be initialized before loading serialized data.");

            values.Clear();
            if (metrics != null)
            {
                foreach (StatSerializedMetric metric in metrics)
                {
                    if (metric == null || string.IsNullOrWhiteSpace(metric.MetricId) || metric.Points == null)
                        continue;

                    if (!TryGetMetricDefinition(metric.MetricId, out StatMetricDefinition definition))
                        continue;

                    Dictionary<StatPointKey, double> metricMap = GetOrCreateMetricMap(metric.MetricId, values);
                    foreach (StatSerializedPoint point in metric.Points)
                    {
                        if (point == null)
                            continue;
                        if (!TryCreateKeyFromSerializedPoint(definition, point.DimensionValues, out StatPointKey key, out double pointValue))
                            continue;
                        metricMap[key] = pointValue;
                    }
                }
            }

            Normalize();
            IsDirty = false;
            Merged = false;
        }

        void RecordInternal(string metricId, double value, byte dimensionCount, StatDimensionValue dim0, StatDimensionValue dim1)
        {
            if (Merged)
            {
                LogUtil.Error("[StatStorage] Record is not allowed after storage has been merged.");
                return;
            }
            if (!TryGetMetricDefinition(metricId, out StatMetricDefinition definition))
                return;
            if (!TryNormalizeInputKey(metricId, definition, dimensionCount, dim0, dim1, out StatPointKey key))
                return;

            ApplyAggregatedValue(metricId, definition, key, value);
        }

        bool TryReadInternal(string metricId, out double value, byte dimensionCount, StatDimensionValue dim0, StatDimensionValue dim1)
        {
            value = 0d;
            if (!TryGetMetricDefinition(metricId, out StatMetricDefinition definition))
                return false;

            int expected = definition.Dimensions != null ? definition.Dimensions.Length : 0;
            if (dimensionCount > expected)
            {
                LogUtil.Warn("[StatStorage] metric '{0}' has {1} dimensions but read got {2}.", metricId, expected, dimensionCount);
                return false;
            }
            if (!values.TryGetValue(metricId, out Dictionary<StatPointKey, double> metricMap))
                return false;

            if (dimensionCount == expected)
            {
                if (!TryNormalizeInputKey(metricId, definition, dimensionCount, dim0, dim1, out StatPointKey exactKey))
                    return false;
                return metricMap.TryGetValue(exactKey, out value);
            }

            StatDimensionValue normalizedDim0 = default(StatDimensionValue);
            StatDimensionValue normalizedDim1 = default(StatDimensionValue);
            if (dimensionCount >= 1 &&
                !TryNormalizeInputDimension(metricId, definition.Dimensions[0], 0, dim0, out normalizedDim0))
                return false;
            if (dimensionCount >= 2 &&
                !TryNormalizeInputDimension(metricId, definition.Dimensions[1], 1, dim1, out normalizedDim1))
                return false;

            bool found = false;
            foreach (KeyValuePair<StatPointKey, double> pointPair in metricMap)
            {
                StatPointKey pointKey = pointPair.Key;
                if (pointKey.DimensionCount != expected)
                    continue;
                if (dimensionCount >= 1 && !pointKey.Dim0.Equals(normalizedDim0))
                    continue;
                if (dimensionCount >= 2 && !pointKey.Dim1.Equals(normalizedDim1))
                    continue;

                if (!found)
                {
                    value = pointPair.Value;
                    found = true;
                }
                else if (definition.AggregateRule == StatAggregateRule.Max)
                {
                    value = Math.Max(value, pointPair.Value);
                }
                else
                {
                    value += pointPair.Value;
                }
            }

            return found;
        }

        bool TryGetMetricDefinition(string metricId, out StatMetricDefinition definition)
        {
            definition = null;
            if (schema == null)
            {
                LogUtil.Warn("[StatStorage] storage is not initialized.");
                return false;
            }
            if (!schema.TryGetMetric(metricId, out definition) || definition == null)
            {
                LogUtil.Warn("[StatStorage] unknown metric '{0}'.", metricId);
                return false;
            }
            if (definition.Disabled)
            {
                LogUtil.Warn("[StatStorage] metric '{0}' is disabled.", metricId);
                return false;
            }
            return true;
        }

        bool TryNormalizeInputKey(string metricId, StatMetricDefinition definition, byte dimensionCount, StatDimensionValue dim0, StatDimensionValue dim1, out StatPointKey key)
        {
            int expected = definition != null && definition.Dimensions != null ? definition.Dimensions.Length : 0;
            if (expected != dimensionCount)
            {
                LogUtil.Warn("[StatStorage] metric '{0}' expects {1} dimensions but got {2}.", metricId, expected, dimensionCount);
                key = default(StatPointKey);
                return false;
            }

            StatDimensionValue normalizedDim0 = default(StatDimensionValue);
            StatDimensionValue normalizedDim1 = default(StatDimensionValue);

            switch (dimensionCount)
            {
                case 0:
                    key = StatPointKey.Empty;
                    return true;

                case 1:
                    if (!TryNormalizeInputDimension(metricId, definition.Dimensions[0], 0, dim0, out normalizedDim0))
                    {
                        key = default(StatPointKey);
                        return false;
                    }
                    key = new StatPointKey(normalizedDim0);
                    return true;

                case 2:
                    if (!TryNormalizeInputDimension(metricId, definition.Dimensions[0], 0, dim0, out normalizedDim0) ||
                        !TryNormalizeInputDimension(metricId, definition.Dimensions[1], 1, dim1, out normalizedDim1))
                    {
                        key = default(StatPointKey);
                        return false;
                    }
                    key = new StatPointKey(normalizedDim0, normalizedDim1);
                    return true;

                default:
                    LogUtil.Warn("[StatStorage] metric '{0}' uses unsupported dimension count {1}.", metricId, dimensionCount);
                    key = default(StatPointKey);
                    return false;
            }
        }

        bool TryNormalizeInputDimension(string metricId, StatDimensionDefinition definition, int index, StatDimensionValue input, out StatDimensionValue normalized)
        {
            normalized = default(StatDimensionValue);
            if (definition == null)
                return false;

            string rawValue;
            switch (definition.ValueType)
            {
                case StatDimensionValueType.Int:
                    if (input.Kind == StatDimensionValueKind.Int)
                        rawValue = input.IntValue.ToString();
                    else if ((input.Kind == StatDimensionValueKind.String || input.Kind == StatDimensionValueKind.TemplateId) && !string.IsNullOrWhiteSpace(input.StringValue))
                        rawValue = input.StringValue;
                    else
                        rawValue = null;
                    break;

                case StatDimensionValueType.String:
                    if (input.Kind == StatDimensionValueKind.Int)
                        rawValue = input.IntValue.ToString();
                    else if (input.Kind == StatDimensionValueKind.String || input.Kind == StatDimensionValueKind.TemplateId)
                        rawValue = input.StringValue;
                    else
                        rawValue = null;
                    break;

                case StatDimensionValueType.TemplateId:
                    if (input.Kind == StatDimensionValueKind.TemplateId || input.Kind == StatDimensionValueKind.String)
                        rawValue = input.StringValue;
                    else
                        rawValue = null;
                    break;

                default:
                    rawValue = null;
                    break;
            }

            if (rawValue == null)
            {
                LogUtil.Warn("[StatStorage] metric '{0}' dimension {1} has unsupported input kind {2}.", metricId, index, input.Kind);
                return false;
            }

            if (!TryNormalizeStoredDimension(definition, rawValue, out string normalizedRaw))
                return false;

            normalized = CreateNormalizedDimensionValue(definition, normalizedRaw);
            return true;
        }

        bool TryNormalizeStoredPoint(StatMetricDefinition definition, StatPointKey rawKey, double rawValue, out StatPointKey normalizedKey, out double normalizedValue)
        {
            normalizedValue = rawValue;
            normalizedKey = default(StatPointKey);
            int expected = definition != null && definition.Dimensions != null ? definition.Dimensions.Length : 0;
            if (rawKey.DimensionCount != expected)
                return false;

            StatDimensionValue normalizedDim0 = default(StatDimensionValue);
            StatDimensionValue normalizedDim1 = default(StatDimensionValue);

            switch (expected)
            {
                case 0:
                    normalizedKey = StatPointKey.Empty;
                    return true;

                case 1:
                    if (!TryNormalizeStoredDimensionValue(definition.Dimensions[0], rawKey.Dim0, out normalizedDim0))
                        return false;
                    normalizedKey = new StatPointKey(normalizedDim0);
                    return true;

                case 2:
                    if (!TryNormalizeStoredDimensionValue(definition.Dimensions[0], rawKey.Dim0, out normalizedDim0) ||
                        !TryNormalizeStoredDimensionValue(definition.Dimensions[1], rawKey.Dim1, out normalizedDim1))
                        return false;
                    normalizedKey = new StatPointKey(normalizedDim0, normalizedDim1);
                    return true;

                default:
                    return false;
            }
        }

        bool TryNormalizeStoredDimensionValue(StatDimensionDefinition definition, StatDimensionValue input, out StatDimensionValue normalized)
        {
            normalized = default(StatDimensionValue);
            string rawValue = input.Kind == StatDimensionValueKind.Int
                ? input.IntValue.ToString()
                : input.StringValue;

            if (!TryNormalizeStoredDimension(definition, rawValue, out string normalizedRaw))
                return false;

            normalized = CreateNormalizedDimensionValue(definition, normalizedRaw);
            return true;
        }

        bool TryNormalizeStoredDimension(StatDimensionDefinition definition, string rawValue, out string normalized)
        {
            normalized = rawValue;
            if (definition == null)
                return false;

            switch (definition.ValueType)
            {
                case StatDimensionValueType.Int:
                    if (!int.TryParse(normalized, out int parsed))
                        return false;
                    if (parsed < definition.IntMin || parsed > definition.IntMax)
                        return false;
                    normalized = parsed.ToString();
                    return true;

                case StatDimensionValueType.TemplateId:
                    if (string.IsNullOrWhiteSpace(normalized))
                        return false;
                    return StatTemplateTypeResolver.Exists(definition.TemplateType, normalized);

                case StatDimensionValueType.String:
                    return normalized != null;

                default:
                    return false;
            }
        }

        void ApplyAggregatedValue(
            string metricId,
            StatMetricDefinition definition,
            StatPointKey key,
            double incomingValue,
            Dictionary<string, Dictionary<StatPointKey, double>> targetValues = null,
            bool markDirty = true)
        {
            if (targetValues == null)
                targetValues = values;
            Dictionary<StatPointKey, double> metricMap = GetOrCreateMetricMap(metricId, targetValues);
            metricMap.TryGetValue(key, out double currentValue);

            double nextValue;
            switch (definition.AggregateRule)
            {
                case StatAggregateRule.Max:
                    nextValue = Math.Max(currentValue, incomingValue);
                    break;

                default:
                    nextValue = currentValue + incomingValue;
                    break;
            }

            metricMap[key] = nextValue;
            if (markDirty)
                IsDirty = true;
        }

        Dictionary<StatPointKey, double> GetOrCreateMetricMap(string metricId, Dictionary<string, Dictionary<StatPointKey, double>> targetValues)
        {
            if (!targetValues.TryGetValue(metricId, out Dictionary<StatPointKey, double> metricMap))
            {
                metricMap = new Dictionary<StatPointKey, double>();
                targetValues[metricId] = metricMap;
            }
            return metricMap;
        }

        bool HasSameSchema(StatStorage other)
        {
            return other != null && ReferenceEquals(schema, other.schema);
        }

        bool TryCreateKeyFromSerializedPoint(StatMetricDefinition definition, object[] dimensionValues, out StatPointKey key, out double value)
        {
            key = default(StatPointKey);
            value = 0d;
            int expected = definition != null && definition.Dimensions != null ? definition.Dimensions.Length : 0;
            int actual = dimensionValues != null ? dimensionValues.Length - 1 : 0;
            if (dimensionValues == null || dimensionValues.Length == 0 || actual != expected)
                return false;
            if (!TryReadSerializedDouble(dimensionValues[0], out value))
                return false;

            switch (expected)
            {
                case 0:
                    key = StatPointKey.Empty;
                    return true;

                case 1:
                    key = new StatPointKey(CreateSerializedDimensionValue(definition.Dimensions[0], dimensionValues[1]));
                    return true;

                case 2:
                    key = new StatPointKey(
                        CreateSerializedDimensionValue(definition.Dimensions[0], dimensionValues[1]),
                        CreateSerializedDimensionValue(definition.Dimensions[1], dimensionValues[2]));
                    return true;

                default:
                    return false;
            }
        }

        static bool TryReadSerializedDouble(object rawValue, out double value)
        {
            switch (rawValue)
            {
                case null:
                    value = 0d;
                    return false;
                case long l:
                    value = l;
                    return true;
                case int i:
                    value = i;
                    return true;
                case double d:
                    value = d;
                    return true;
                case float f:
                    value = f;
                    return true;
                case decimal m:
                    value = (double)m;
                    return true;
                case string s when double.TryParse(s, out double parsed):
                    value = parsed;
                    return true;
                default:
                    value = 0d;
                    return false;
            }
        }

        static StatDimensionValue CreateSerializedDimensionValue(StatDimensionDefinition definition, object rawValue)
        {
            switch (definition.ValueType)
            {
                case StatDimensionValueType.Int:
                    return StatDimensionValue.FromInt(ReadSerializedInt(rawValue));

                case StatDimensionValueType.TemplateId:
                    return StatDimensionValue.FromTemplateId(rawValue != null ? rawValue.ToString() : null);

                default:
                    return StatDimensionValue.FromString(rawValue != null ? rawValue.ToString() : null);
            }
        }

        static int ReadSerializedInt(object rawValue)
        {
            switch (rawValue)
            {
                case long l:
                    return (int)l;
                case int i:
                    return i;
                case string s when int.TryParse(s, out int parsed):
                    return parsed;
                default:
                    return 0;
            }
        }

        static StatDimensionValue CreateNormalizedDimensionValue(StatDimensionDefinition definition, string rawValue)
        {
            switch (definition.ValueType)
            {
                case StatDimensionValueType.Int:
                    return StatDimensionValue.FromInt(int.Parse(rawValue));

                case StatDimensionValueType.TemplateId:
                    return StatDimensionValue.FromTemplateId(rawValue);

                default:
                    return StatDimensionValue.FromString(rawValue);
            }
        }
    }

    internal sealed class StatSerializedMetric
    {
        public string MetricId;
        public List<StatSerializedPoint> Points = new List<StatSerializedPoint>();
    }

    internal sealed class StatSerializedPoint
    {
        public object[] DimensionValues;
    }
}
