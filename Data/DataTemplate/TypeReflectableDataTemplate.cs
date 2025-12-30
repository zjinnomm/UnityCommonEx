using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityCommonEx
{

    public abstract class TypeReflectableDataTemplate<T, E> : BaseDataTemplate where E : Enum where T : TypeReflectableDataTemplate<T, E>
    {

        static bool _typeReflectionInit = false;
        static Dictionary<E, Type> _enumToTypeMap = new Dictionary<E, Type>();
        static Dictionary<Type, E> _typeToEnumMap = new Dictionary<Type, E>();

        protected static void RegisterTypeReflection(Type type, E enumValue)
        {
            _enumToTypeMap.Add(enumValue, type);
            if (!_typeToEnumMap.ContainsKey(type))
            {
                _typeToEnumMap.Add(type, enumValue);
            }
        }

        static void InitTypeReflection()
        {
            if (_typeReflectionInit)
            {
                return;
            }
            _typeReflectionInit = true;
            typeof(T).GetMethod("RegisterTypeReflections").Invoke(null, null);
        }

        public TypeReflectableDataTemplate()
        {
            Type = GetEnumReflection(GetType());
        }

        public static Type GetTypeReflection(E enumValue)
        {
            InitTypeReflection();
            return _enumToTypeMap[enumValue];
        }

        public static E GetEnumReflection(Type type)
        {
            InitTypeReflection();
            return _typeToEnumMap[type];
        }

        public static T CreateNew(E enumValue)
        {
            T t = Activator.CreateInstance(GetTypeReflection(enumValue)) as T;
            if (t != null)
            {
                t.Type = enumValue;
            }
            return t;
        }

        public E Type;

    }

    public class TypeReflectableDataTemplateConverter<T, E> : JsonConverter<T> where T : TypeReflectableDataTemplate<T, E> where E : Enum
    {
        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            JObject jo = JObject.Load(reader);
            JToken token = jo.ContainsKey("type") ? jo["type"] : jo["Type"];
            if (token == null || token.Type != JTokenType.String)
            {
                return null;
            }
            E enumValue = (E)Enum.Parse(typeof(E), token.Value<string>(), true);
            object instance = TypeReflectableDataTemplate<T, E>.CreateNew(enumValue);
            serializer.Populate(jo.CreateReader(), instance);
            return (T)instance;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}