using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityCommonEx
{
    /// <summary>
    /// 配置一个静态方法引用（格式 ClassName.MethodName），Init 时通过反射解析并缓存，触发时直接调用。
    /// 模板由参数列表与返回类型决定，通过 Init 传入。
    /// </summary>
    [JsonConverter(typeof(CachedStaticMethodRefConverter))]
    public class CachedStaticMethodRef : IDataTableStringField
    {
        /// <summary>
        /// 函数名（格式 ClassName.MethodName），仅由 JSON 从 string 反序列化得到。
        /// </summary>
        public string Function { get; set; }

        private MethodInfo _method;

        /// <summary>
        /// 是否已成功解析并缓存。
        /// </summary>
        public bool IsResolved => _method != null;

        /// <summary>
        /// 解析并缓存静态方法，签名必须匹配 parameterTypes 与 returnType。
        /// null/空字符串时不报错直接返回；非空但反射不到类型/方法/签名不对时才报错。
        /// </summary>
        public void Init(Type[] parameterTypes, Type returnType, string errorContext = null)
        {
            if (string.IsNullOrEmpty(Function))
                return;

            string[] parts = Function.Split('.');
            if (parts.Length != 2)
            {
                LogUtil.Error($"{errorContext}: Invalid Function format '{Function}'. Expected 'ClassName.MethodName'");
                return;
            }

            string className = parts[0];
            string methodName = parts[1];

            Type classType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                classType = assembly.GetType(className);
                if (classType != null)
                    break;
            }

            if (classType == null)
            {
                LogUtil.Error($"{errorContext}: Class '{className}' not found for Function '{Function}'");
                return;
            }

            var methodInfo = classType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                parameterTypes,
                null);

            if (methodInfo == null)
            {
                LogUtil.Error($"{errorContext}: Method '{methodName}' not found in class '{className}' for Function '{Function}' (signature mismatch)");
                return;
            }

            if (methodInfo.ReturnType != returnType)
            {
                LogUtil.Error($"{errorContext}: Method '{methodName}' in class '{className}' must return {returnType.Name} for Function '{Function}'");
                return;
            }

            _method = methodInfo;
        }

        /// <summary>
        /// 调用缓存的静态方法；失败或异常时返回 false。
        /// </summary>
        public bool TryInvoke(object[] args, out object result)
        {
            result = null;
            if (_method == null)
                return false;
            try
            {
                result = _method.Invoke(null, args);
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error($"CachedStaticMethodRef.Invoke '{Function}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 调用并强转返回值为 T；失败或类型不匹配时返回 false。
        /// </summary>
        public bool TryInvoke<T>(object[] args, out T result)
        {
            result = default;
            if (!TryInvoke(args, out object raw))
                return false;
            if (raw is T t)
            {
                result = t;
                return true;
            }
            return false;
        }

        public void SetFromString(string value)
        {
            Function = value;
        }
    }

    /// <summary>
    /// 仅支持从 string 反序列化（格式 ClassName.MethodName），不支持序列化。
    /// </summary>
    public class CachedStaticMethodRefConverter : JsonConverter<CachedStaticMethodRef>
    {
        public override CachedStaticMethodRef ReadJson(JsonReader reader, Type objectType, CachedStaticMethodRef existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.String)
            {
                var s = reader.Value?.ToString();
                return new CachedStaticMethodRef { Function = s };
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var jo = JObject.Load(reader);
                var token = jo["Function"] ?? jo["function"];
                var str = token?.Value<string>();
                return new CachedStaticMethodRef { Function = str };
            }

            return null;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, CachedStaticMethodRef value, JsonSerializer serializer)
        {
            throw new NotImplementedException("CachedStaticMethodRef 不支持序列化");
        }
    }
}
