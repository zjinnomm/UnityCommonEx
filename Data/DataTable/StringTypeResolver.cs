using System;
using Unity.VisualScripting;

namespace UnityCommonEx
{
    public static class StringTypeResolver
    {

        public static Func<string, object> GetResolver(Type type)
        {
            if (type == typeof(string))
            {
                return (s) => s;
            }
            if (type == typeof(int))
            {
                return (s) => int.Parse(s);
            }
            if (type == typeof(long))
            {
                return (s) => long.Parse(s);
            }
            if (type == typeof(float))
            {
                return (s) => float.Parse(s);
            }
            if (type == typeof(double))
            {
                return (s) => double.Parse(s);
            }
            if (type == typeof(bool))
            {
                return (s) => bool.Parse(s);
            }
            if (type.IsEnum)
            {
                return (s) => Enum.Parse(type, s);
            }
            if (type == typeof(FloatRange))
            {
                return (s) => FloatRange.FromString(s);
            }
            if (type == typeof(IntRange))
            {
                return (s) => IntRange.FromString(s);
            }
            if (typeof(ILevelDataTemplateReference).IsAssignableFrom(type))
            {
                return (s) =>
                {
                    ILevelDataTemplateReference result = Activator.CreateInstance(type) as ILevelDataTemplateReference;
                    string[] split = s.Split(":");
                    result.Id = split[0];
                    result.Level = split.Length > 1 ? int.Parse(split[1]) : 0;
                    return result;
                };
            }
            if (typeof(IDataTableReference).IsAssignableFrom(type))
            {
                return (s) =>
                {
                    object result = Activator.CreateInstance(type);
                    ((IDataTableReference)result).Id = s;
                    return result;
                };
            }
            if (typeof(IDataTemplateReference).IsAssignableFrom(type))
            {
                return (s) =>
                {
                    object result = Activator.CreateInstance(type);
                    ((IDataTemplateReference)result).Id = s;
                    return result;
                };
            }
            
            return (s) => typeof(JsonUtil).GetMethod("ReadRaw").MakeGenericMethod(type).InvokeOptimized(null, s);
        }

    }
}