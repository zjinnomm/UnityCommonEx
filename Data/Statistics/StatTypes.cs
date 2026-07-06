using System;

namespace UnityCommonEx
{
    public enum StatAggregateRule
    {
        Sum = 0,
        Max
    }

    public enum StatDimensionValueType
    {
        Int = 0,
        String,
        TemplateId
    }

    public enum StatDimensionValueKind
    {
        None = 0,
        Int,
        String,
        TemplateId
    }

    public struct StatDimensionValue : IEquatable<StatDimensionValue>
    {
        static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        public StatDimensionValueKind Kind;
        public int IntValue;
        public string StringValue;

        public static StatDimensionValue FromInt(int value)
        {
            return new StatDimensionValue
            {
                Kind = StatDimensionValueKind.Int,
                IntValue = value,
                StringValue = null
            };
        }

        public static StatDimensionValue FromString(string value)
        {
            return new StatDimensionValue
            {
                Kind = StatDimensionValueKind.String,
                IntValue = 0,
                StringValue = value
            };
        }

        public static StatDimensionValue FromTemplateId(string templateId)
        {
            return new StatDimensionValue
            {
                Kind = StatDimensionValueKind.TemplateId,
                IntValue = 0,
                StringValue = templateId
            };
        }

        public static StatDimensionValue FromTemplate<T>(T template) where T : BaseDataTemplate
        {
            return FromTemplateId(template != null ? template.Id : null);
        }

        public bool Equals(StatDimensionValue other)
        {
            if (Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case StatDimensionValueKind.Int:
                    return IntValue == other.IntValue;

                case StatDimensionValueKind.String:
                case StatDimensionValueKind.TemplateId:
                    return Comparer.Equals(StringValue, other.StringValue);

                default:
                    return true;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is StatDimensionValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                switch (Kind)
                {
                    case StatDimensionValueKind.Int:
                        return (hash * 31) + IntValue;

                    case StatDimensionValueKind.String:
                    case StatDimensionValueKind.TemplateId:
                        return (hash * 31) + Comparer.GetHashCode(StringValue ?? string.Empty);

                    default:
                        return hash;
                }
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case StatDimensionValueKind.Int:
                    return IntValue.ToString();

                case StatDimensionValueKind.String:
                case StatDimensionValueKind.TemplateId:
                    return StringValue ?? string.Empty;

                default:
                    return string.Empty;
            }
        }
    }
}
