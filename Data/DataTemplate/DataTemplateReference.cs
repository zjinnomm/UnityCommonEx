using System;
using Newtonsoft.Json;
using Unity.VisualScripting;

namespace UnityCommonEx
{

    public interface IDataTemplateReference
    {
        public string Id { set; get; }
    }

    [Serializable]
    public struct DataTemplateReference<T> : IDataTemplateReference, IEquatable<DataTemplateReference<T>> where T : BaseDataTemplate
    {

        private string id;
        
        public T Data
        {
            get
            {
                if (cachedResolved == null || cachedResolved.Id != id)
                {
                    if (id == null)
                    {
                        return null;
                    }
                    cachedResolved = DataTemplateManager.Get<T>(id);
                }
                return cachedResolved;
            }
        }

        public string Id 
        { 
            get => id; 
            set 
            {
                if (value != id)
                {
                    id = value;
                    cachedResolved = null;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                if (cachedResolved != null)
                {
                    return true;
                }
                if (id == null)
                {
                    return false;
                }
                cachedResolved = DataTemplateManager.Get<T>(id);
                return cachedResolved != null;
            }
        }

        [NonSerialized]
        T cachedResolved;

        public bool Equals(DataTemplateReference<T> other)
        {
            return id.Equals(other.id);
        }

        public DataTemplateReference(string id)
        {
            this.id = id;
            cachedResolved = null;
        }

    }

    public class DataTemplateReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDataTemplateReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object result = Activator.CreateInstance(objectType);
            ((IDataTemplateReference)result).Id = (string) reader.Value;
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((IDataTemplateReference)value).Id);
        }
    }

}