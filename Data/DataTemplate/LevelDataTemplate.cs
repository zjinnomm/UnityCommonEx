using Newtonsoft.Json;
using System;

namespace UnityCommonEx
{

    public interface ILevelDataTemplate
    {

        public int GetMaxLevel();

    }

    public abstract class LevelDataTemplate<T> : BaseDataTemplate, ILevelDataTemplate where T : IDifferencialArrayElement
    {

        public T[] LevelData;

        public int GetMaxLevel() => LevelData.Length;

    }

    public interface ILevelDataTemplateReference : IDataTableStringField
    {
        public string Id { set; get; }
        public int Level { set; get; }
    }

    public struct LevelDataTemplateReference<T> : ILevelDataTemplateReference, IEquatable<LevelDataTemplateReference<T>> where T : BaseDataTemplate, ILevelDataTemplate
    {

        private string id;
        private int level;

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

        public int Level { get => level; set => level = value; }

        public bool IsValid
        {
            get
            {
                if (cachedResolved == null)
                {
                    if (id == null)
                    {
                        return false;
                    }
                    cachedResolved = DataTemplateManager.Get<T>(id);
                }
                return cachedResolved != null && Level >= 0 && cachedResolved.GetMaxLevel() > Level;
            }
        }

        

        [NonSerialized]
        T cachedResolved;

        public bool Equals(LevelDataTemplateReference<T> other)
        {
            return id.Equals(other.id) && Level == other.Level;
        }

        public LevelDataTemplateReference(string id, int level)
        {
            this.id = id;
            this.level = level;
            cachedResolved = null;
        }

        public void SetFromString(string value)
        {
            string[] split = value.Split(":");
            Id = split[0];
            Level = split.Length > 1 ? int.Parse(split[1]) : 0;
        }

    }

    public class LevelDataTemplateReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ILevelDataTemplateReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            ILevelDataTemplateReference result = Activator.CreateInstance(objectType) as ILevelDataTemplateReference;
            string[] split = ((string)reader.Value).Split(":");
            result.Id = split[0];
            result.Level = split.Length > 1 ? int.Parse(split[1]) : 0;
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ILevelDataTemplateReference reference = value as ILevelDataTemplateReference;
            writer.WriteValue($"{reference.Id}:{reference.Level}");
        }
    }



}
