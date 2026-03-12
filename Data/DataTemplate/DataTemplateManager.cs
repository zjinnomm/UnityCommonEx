using System;
using System.Collections.Generic;
using System.IO;

namespace UnityCommonEx
{

    public static class DataTemplateManager
    {

        public static void Validate<T>() where T : BaseDataTemplate
        {
            var list = GetAll<T>();
            if (list != null) 
            {
                foreach (var template in list)
                {
                    string msg = template.Validate();
                    if (msg != null)
                    {
                        LogUtil.Error("DataTemplate {0} {1} Error: {2}", typeof(T), template.Id, msg);
                        return;
                    }
                }
            }
        }

        public static T Get<T>(string id) where T : BaseDataTemplate
        {
            return DataTemplateManagerInstance<T>.Get(id);
        }

        public static T Get<T>() where T : BaseDataTemplate
        {
            return DataTemplateManagerInstance<T>.Get();
        }

        public static ICollection<T> GetAll<T>() where T : BaseDataTemplate
        {
            return DataTemplateManagerInstance<T>.GetAll();
        }

        public static T LoadSingleRaw<T>(string title, string content) where T : BaseDataTemplate
        {
            return DataTemplateManagerInstance<T>.LoadSingleRaw(title, content);
        }

        public static T LoadSingle<T>(string path) where T : BaseDataTemplate
        {
            return DataTemplateManagerInstance<T>.LoadSingle(path);
        }

        public static void LoadExt<T>(string path, string ext) where T : BaseDataTemplate
        {
            DataTemplateManagerInstance<T>.LoadExt(path, ext);
        }

        public static void Clear<T>() where T : BaseDataTemplate
        {
            DataTemplateManagerInstance<T>.Clear();
        }

    }

    public static class DataTemplateManagerInstance<T> where T : BaseDataTemplate
    {

        static Dictionary<string, T> Entries;
        static T DefaultEntry;

        public static void Clear()
        {
            if (Entries != null)
            {
                Entries.Clear();
            }
            DefaultEntry = null;
        }

        public static T LoadSingleRaw(string title, string content)
        {
            if (Entries == null)
            {
                Entries = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            }
            var entry = JsonUtil.ReadRaw<T>(content);
            if (entry == null)
            {
                LogUtil.Error("DataTemplate {0} {1} Error: {2}", typeof(T), title, "null");
                return null;
            }
            entry.PostInit();
            if (entry.Id == null)
            {
                entry.Id = title;
            }
            if (Entries.ContainsKey(entry.Id))
            {
                LogUtil.Warn("{0} load duplicate entry {1}", typeof(T).Name, entry.Id);
                Entries[entry.Id] = entry;
            }
            else
            {
                Entries.Add(entry.Id, entry);
            }
            DefaultEntry = entry;
            return entry;
        }

        public static T LoadSingle(string path)
        {
            try
            {
                if (Entries == null)
                {
                    Entries = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
                }
                var entry = JsonUtil.Read<T>(path);
                if (entry == null)
                {
                    LogUtil.Error("DataTemplate {0} 文件加载为 null, 文件: {1}", typeof(T), path);
                    return default;
                }
                entry.PostInit();
                if (entry.Id == null)
                {
                    entry.Id = Path.GetFileName(path).Split(".")[0];
                }
                if (Entries.ContainsKey(entry.Id))
                {
                    LogUtil.Warn("{0} load duplicate entry {1}", typeof(T).Name, entry.Id);
                    Entries[entry.Id] = entry;
                }
                else
                {
                    Entries.Add(entry.Id, entry);
                }
                DefaultEntry = entry;
                return entry;
            }
            catch (Exception ex)
            {
                LogUtil.Error("DataTemplate LoadSingle 失败, 类型: {0}, 文件: {1}, 错误: {2}", typeof(T).Name, path, ex);
                return default;
            }
        }

        public static void LoadExt(string path, string ext)
        {
            foreach (var p in Directory.EnumerateFiles(path, $"*.{ext}.json", SearchOption.AllDirectories))
            {
                LoadSingle(p);
            }
        }

        public static T Get()
        {
            return DefaultEntry;    
        }

        public static T Get(string id)
        {
            return Entries?.GetValueOrDefault(id, null);
        }

        public static ICollection<T> GetAll()
        {
            return Entries?.Values;
        }

    }

}
