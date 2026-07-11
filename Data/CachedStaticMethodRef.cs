using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityCommonEx
{
    /// <summary>
    /// Stores a static method reference in the ClassName.MethodName format.
    /// Init resolves and caches the method for later invocation.
    /// </summary>
    [JsonConverter(typeof(CachedStaticMethodRefConverter))]
    public class CachedStaticMethodRef : IDataTableStringField
    {
        /// <summary>
        /// Static method name in the ClassName.MethodName format.
        /// </summary>
        public string Function { get; set; }

        private MethodInfo _method;

        /// <summary>
        /// Whether the referenced method has been resolved and cached.
        /// </summary>
        public bool IsResolved => _method != null;

        /// <summary>
        /// Resolves and caches the referenced static method using the required signature.
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

            if (methodInfo.ReturnType != returnType && !(returnType == typeof(UnlockProgress) && methodInfo.ReturnType == typeof(bool)))
            {
                LogUtil.Error($"{errorContext}: Method '{methodName}' in class '{className}' must return {returnType.Name} for Function '{Function}'");
                return;
            }

            _method = methodInfo;
        }

        /// <summary>
        /// Invokes the cached static method and returns false when invocation fails.
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
        /// Invokes the cached method and converts its return value to T.
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
    /// Converts the ClassName.MethodName string representation to a method reference.
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
            throw new NotImplementedException("CachedStaticMethodRef does not support serialization.");
        }
    }
}