using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityCommonEx
{
    public sealed class StatDimensionDefinition
    {
        public StatDimensionValueType ValueType;
        public int IntMin = int.MinValue;
        public int IntMax = int.MaxValue;
        public string TemplateTypeName;
        public Type TemplateType;
    }

    public sealed class StatMetricDefinition
    {
        public string MetricId;
        public MultiLingualText Title;
        public StatAggregateRule AggregateRule;
        public StatDimensionDefinition[] Dimensions = Array.Empty<StatDimensionDefinition>();
        public bool Disabled;
    }

    public sealed class StatSchemaSet
    {
        readonly Dictionary<string, StatMetricDefinition> metricDefs;

        public ICollection<StatMetricDefinition> Metrics => metricDefs.Values;

        public StatSchemaSet(IEnumerable<StatMetricDefinition> definitions)
        {
            metricDefs = new Dictionary<string, StatMetricDefinition>(StringComparer.OrdinalIgnoreCase);
            if (definitions == null)
                return;

            foreach (StatMetricDefinition def in definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.MetricId))
                    continue;
                metricDefs[def.MetricId] = def;
            }
        }

        public bool TryGetMetric(string metricId, out StatMetricDefinition definition)
        {
            if (string.IsNullOrEmpty(metricId))
            {
                definition = null;
                return false;
            }

            return metricDefs.TryGetValue(metricId, out definition);
        }

        public static StatSchemaSet FromTable(DataTable<string, StatSchemaRow> table)
        {
            var definitions = new List<StatMetricDefinition>();
            if (table == null)
                return new StatSchemaSet(definitions);

            for (int i = 0; i < table.Count; i++)
            {
                StatSchemaRow row = table[i];
                if (row == null || string.IsNullOrEmpty(row.MetricId))
                    continue;
                definitions.Add(row.ToDefinition());
            }

            return new StatSchemaSet(definitions);
        }

        public static StatSchemaSet FromTableId(string tableId)
        {
            return FromTable(DataTableManager.Get<string, StatSchemaRow>(tableId));
        }

        public static StatSchemaSet FromDefaultTable()
        {
            return FromTable(DataTableManager.Get<string, StatSchemaRow>());
        }
    }

    public sealed class StatisticSchemaManager
    {
        public const string BuiltInDefaultTemplate = "AllStatistics";

        static readonly StatisticSchemaManager instance = new StatisticSchemaManager();
        readonly Dictionary<string, StatSchemaSet> schemaByTemplate = new Dictionary<string, StatSchemaSet>(StringComparer.OrdinalIgnoreCase);

        string defaultTemplate = BuiltInDefaultTemplate;

        public static StatisticSchemaManager Instance => instance;
        public string DefaultTemplate => defaultTemplate;
        public bool IsInitialized { get; private set; }

        StatisticSchemaManager()
        {
        }

        public void Initialize(IEnumerable<string> templates, string defaultSchemaTemplate = null)
        {
            schemaByTemplate.Clear();
            defaultTemplate = string.IsNullOrWhiteSpace(defaultSchemaTemplate) ? BuiltInDefaultTemplate : defaultSchemaTemplate;

            if (templates != null)
            {
                foreach (string template in templates)
                    Register(template);
            }

            IsInitialized = true;
        }

        public void Clear()
        {
            schemaByTemplate.Clear();
            defaultTemplate = BuiltInDefaultTemplate;
            IsInitialized = false;
        }

        public string ResolveTemplate(string template)
        {
            return string.IsNullOrWhiteSpace(template) ? defaultTemplate : template;
        }

        public void Register(string template)
        {
            string resolvedTemplate = ResolveTemplate(template);
            schemaByTemplate[resolvedTemplate] = CreateSchemaSet(resolvedTemplate);
        }

        public void Register(string template, StatSchemaSet schemaSet)
        {
            string resolvedTemplate = ResolveTemplate(template);
            schemaByTemplate[resolvedTemplate] = schemaSet ?? new StatSchemaSet(null);
        }

        public bool TryGet(string template, out StatSchemaSet schemaSet)
        {
            string resolvedTemplate = ResolveTemplate(template);
            if (!schemaByTemplate.TryGetValue(resolvedTemplate, out schemaSet))
            {
                schemaSet = CreateSchemaSet(resolvedTemplate);
                schemaByTemplate[resolvedTemplate] = schemaSet;
            }

            return schemaSet != null;
        }

        public StatSchemaSet Get(string template = null)
        {
            TryGet(template, out StatSchemaSet schemaSet);
            return schemaSet ?? new StatSchemaSet(null);
        }

        StatSchemaSet CreateSchemaSet(string template)
        {
            DataTable<string, StatSchemaRow> table;
            if (string.Equals(template, BuiltInDefaultTemplate, StringComparison.OrdinalIgnoreCase))
                table = DataTableManager.Get<string, StatSchemaRow>(template) ?? DataTableManager.Get<string, StatSchemaRow>();
            else
                table = DataTableManager.Get<string, StatSchemaRow>(template);

            if (table == null)
                LogUtil.Warn("[StatisticSchemaManager] schema table '{0}' is missing. Using empty schema.", template);

            return StatSchemaSet.FromTable(table);
        }
    }

    public static class StatSchemaRegistry
    {
        public const string DefaultTableId = StatisticSchemaManager.BuiltInDefaultTemplate;

        public static StatSchemaSet GetDefault()
        {
            return StatisticSchemaManager.Instance.Get();
        }

        public static StatSchemaSet Get(string tableId)
        {
            return StatisticSchemaManager.Instance.Get(tableId);
        }
    }

    public sealed class StatSchemaRow : BaseDataTableRow<string>
    {
        public string MetricId;
        public MultiLingualText Title;
        public StatAggregateRule AggregateRule;
        public int DimensionCount;
        public StatDimensionValueType[] DimensionValueTypes;
        public int[] DimensionIntMin;
        public int[] DimensionIntMax;
        public string[] DimensionTemplateTypes;
        public bool Disabled;

        public override string RowKey => MetricId;

        public override string Validate()
        {
            if (string.IsNullOrWhiteSpace(MetricId))
                return "MetricId is empty.";
            if (DimensionCount < 0)
                return "DimensionCount must be >= 0.";
            if (DimensionCount > 2)
                return "DimensionCount must be <= 2 in the current framework version.";

            for (int i = 0; i < DimensionCount; i++)
            {
                StatDimensionValueType valueType = GetArrayValue(DimensionValueTypes, i, StatDimensionValueType.String);
                int min = GetArrayValue(DimensionIntMin, i, int.MinValue);
                int max = GetArrayValue(DimensionIntMax, i, int.MaxValue);
                if (valueType == StatDimensionValueType.Int && min > max)
                    return $"Dimension {i} int range is invalid.";

                if (valueType == StatDimensionValueType.TemplateId)
                {
                    string templateTypeName = GetArrayValue(DimensionTemplateTypes, i, null);
                    if (string.IsNullOrWhiteSpace(templateTypeName))
                        return $"Dimension {i} TemplateType is empty.";
                    if (StatTemplateTypeResolver.ResolveTemplateType(templateTypeName) == null)
                        return $"Dimension {i} TemplateType '{templateTypeName}' cannot be resolved.";
                }
            }

            return null;
        }

        public StatMetricDefinition ToDefinition()
        {
            int count = Math.Max(0, DimensionCount);
            var dims = new StatDimensionDefinition[count];
            for (int i = 0; i < count; i++)
            {
                string templateTypeName = GetArrayValue(DimensionTemplateTypes, i, null);
                dims[i] = new StatDimensionDefinition
                {
                    ValueType = GetArrayValue(DimensionValueTypes, i, StatDimensionValueType.String),
                    IntMin = GetArrayValue(DimensionIntMin, i, int.MinValue),
                    IntMax = GetArrayValue(DimensionIntMax, i, int.MaxValue),
                    TemplateTypeName = templateTypeName,
                    TemplateType = StatTemplateTypeResolver.ResolveTemplateType(templateTypeName)
                };
            }

            return new StatMetricDefinition
            {
                MetricId = MetricId,
                Title = Title,
                AggregateRule = AggregateRule,
                Dimensions = dims,
                Disabled = Disabled
            };
        }

        static T GetArrayValue<T>(T[] array, int index, T defaultValue)
        {
            if (array == null || index < 0 || index >= array.Length)
                return defaultValue;
            return array[index];
        }
    }

    static class StatTemplateTypeResolver
    {
        static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<Type, MethodInfo> GetterCache = new Dictionary<Type, MethodInfo>();

        public static Type ResolveTemplateType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;
            if (TypeCache.TryGetValue(typeName, out Type cached))
                return cached;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type resolved = assembly.GetType(typeName);
                if (resolved == null)
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (!string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        resolved = type;
                        break;
                    }
                }

                if (resolved == null || !typeof(BaseDataTemplate).IsAssignableFrom(resolved))
                    continue;

                TypeCache[typeName] = resolved;
                return resolved;
            }

            TypeCache[typeName] = null;
            return null;
        }

        public static bool Exists(Type templateType, string templateId)
        {
            if (templateType == null || string.IsNullOrWhiteSpace(templateId))
                return false;

            if (!GetterCache.TryGetValue(templateType, out MethodInfo getter))
            {
                getter = typeof(DataTemplateManager).GetMethod(nameof(DataTemplateManager.Get), new[] { typeof(string) })?.MakeGenericMethod(templateType);
                GetterCache[templateType] = getter;
            }

            if (getter == null)
                return false;

            object resolved = getter.Invoke(null, new object[] { templateId });
            return resolved != null;
        }
    }
}