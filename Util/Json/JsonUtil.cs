using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UnityCommonEx
{
    public enum JsonReadFailureMode
    {
        Error,
        Warning
    }

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

        public static T Read<T>(string path, JsonReadFailureMode failureMode = JsonReadFailureMode.Error)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(NormalizeJsonContent(File.ReadAllText(path)), defaultSettings);
            }
            catch (Exception ex)
            {
                if (failureMode == JsonReadFailureMode.Warning)
                {
                    LogUtil.Warn(
                        "JsonUtil.Read failed for {0} at '{1}': {2}",
                        typeof(T).Name,
                        path,
                        ex.Message);
                    return default;
                }

                LogUtil.Error(
                    "JsonUtil.Read failed for {0} at '{1}': {2}",
                    typeof(T).Name,
                    path,
                    ex.Message);
                return default;
            }
        }

        public static T ReadRaw<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(NormalizeJsonContent(content), defaultSettings);
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

        static string NormalizeJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            if (content[0] == '\uFEFF')
                return content.Substring(1);

            return content;
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
