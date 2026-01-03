using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    /// <summary>
    /// 响应式属性包装器，提供 set/get 和 OnChange 回调
    /// </summary>
    [JsonConverter(typeof(ReactivePropertyConverter))]
    public struct ReactiveProperty<T>
    {
        private T value;
        private event Action<T, T> onChange;
        private static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        /// <summary>
        /// 当前值
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                if (!comparer.Equals(this.value, value))
                {
                    T oldValue = this.value;
                    this.value = value;
                    onChange?.Invoke(oldValue, value);
                }
            }
        }

        /// <summary>
        /// 值变化回调 (oldValue, newValue)
        /// </summary>
        public event Action<T, T> OnChange
        {
            add => onChange += value;
            remove => onChange -= value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ReactiveProperty(T initialValue)
        {
            value = initialValue;
            onChange = null;
        }

        /// <summary>
        /// 隐式转换到 T
        /// </summary>
        public static implicit operator T(ReactiveProperty<T> property)
        {
            return property.value;
        }

        /// <summary>
        /// 隐式转换从 T
        /// </summary>
        public static implicit operator ReactiveProperty<T>(T value)
        {
            return new ReactiveProperty<T>(value);
        }
    }

    /// <summary>
    /// ReactiveProperty 的 JSON 转换器
    /// </summary>
    public class ReactivePropertyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // 检查是否是 ReactiveProperty<> 类型
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // 获取泛型参数类型
            Type valueType = objectType.GetGenericArguments()[0];
            
            // 读取值
            object value = serializer.Deserialize(reader, valueType);
            
            // 创建 ReactiveProperty 实例
            Type reactivePropertyType = typeof(ReactiveProperty<>).MakeGenericType(valueType);
            return Activator.CreateInstance(reactivePropertyType, value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 获取 Value 属性并序列化
            Type reactivePropertyType = value.GetType();
            var valueProperty = reactivePropertyType.GetProperty("Value");
            object propertyValue = valueProperty.GetValue(value);
            serializer.Serialize(writer, propertyValue);
        }
    }
}

