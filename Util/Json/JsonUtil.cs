using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UnityCommonEx
{
    public static class JsonUtil
    {

        static JsonSerializerSettings defaultSettings;

        static JsonUtil()
        {
            defaultSettings = new JsonSerializerSettings();
            defaultSettings.MissingMemberHandling = MissingMemberHandling.Error;
            defaultSettings.ContractResolver = new OverrideToDefaultContractResolver();
            defaultSettings.Converters = new JsonConverter[] {
                new UnityArrayTypeConverter(),
                new EnumConverter(),
                new DataTemplateReferenceConverter(),
                new DataTableReferenceConverter(),
                new DataTableRowReferenceConverter(),
                new LevelDataTemplateReferenceConverter(),
                new DifferencialArrayConverter(),
            };
        }

        public static T Read<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), defaultSettings);
        }

        public static T ReadRaw<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content, defaultSettings);
        }

        public static void Write(string path, object obj)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented, defaultSettings));
        }

        public static void Write<T>(string path, T obj)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented, defaultSettings));
        }

        public static string WriteRaw<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, defaultSettings);
        }

    }

    public class OverrideToDefaultContractResolver : DefaultContractResolver
    {

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            contract.ItemRequired = Required.Always;
            return contract;
        }

    }

}