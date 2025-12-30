using System;
using System.Collections.Generic;
using System.IO;

namespace UnityCommonEx
{

    [Serializable]
    public class ScatteredDataLoadConfig
    {

        public string BaseDir;
        public Dictionary<string, string[]> Entries;

        struct TypeConfig
        {
            public Type KeyType;
            public Type DataType;
            public string[] Units;
        }

        List<TypeConfig> TypeConfigs;

        public void Prepare()
        {
            if (TypeConfigs == null)
            {
                TypeConfigs = new List<TypeConfig>();
            }
            TypeConfigs.Clear();
            foreach (var pair in Entries)
            {
                string[] types = pair.Key.Split(":");
                if (types.Length > 2)
                {
                    LogUtil.Error("too many types to unpack");
                    return;
                }
                Type dataType = Type.GetType(types[types.Length - 1]);
                Type keyType = null;

                if (types.Length == 2)
                {
                    keyType = Type.GetType(types[0]);
                    if (keyType == null)
                    {
                        LogUtil.Error("cannot reflect key type {0}", types[0]);
                        return;
                    }
                }

                if (dataType == null)
                {
                    LogUtil.Error("type {0} does not exist.", pair.Key);
                    return;
                }

                if (keyType == null)
                {
                    if (!typeof(BaseDataTemplate).IsAssignableFrom(dataType))
                    {
                        LogUtil.Error("type {0} is not a DataTemplate.", dataType);
                        return;
                    }
                }
                else
                {
                    if (!typeof(BaseDataTableRow<>).MakeGenericType(keyType).IsAssignableFrom(dataType))
                    {
                        LogUtil.Error("type {0} is not a DataRow with key type {1}.", dataType, keyType);
                        return;
                    }
                }
                
                if (pair.Value == null || pair.Value.Length == 0)
                {
                    LogUtil.Error("type {0}'s load map is empty.", pair.Key);
                    return;
                }

                TypeConfigs.Add(new TypeConfig{
                    KeyType = keyType,
                    DataType = dataType,
                    Units = pair.Value
                });
            }
        }

        public void Load(bool Validate = true)
        {
            if (TypeConfigs == null)
            {
                return;
            }
            foreach (var config in TypeConfigs)
            {
                DataManager.Clear(config.KeyType, config.DataType);

                foreach (string s in config.Units)
                {
                    if (s.Contains("."))
                    {
                        DataManager.LoadByPath(config.KeyType, config.DataType, Path.Join(BaseDir, s));
                    }
                    else
                    {
                        DataManager.LoadByExt(config.KeyType, config.DataType, BaseDir, s);
                    }
                }
            }

            if (Validate)
            {
                foreach (var config in TypeConfigs)
                {
                    DataManager.Validate(config.KeyType, config.DataType);

                }
            }
        }

        public void Dump(Stream stream)
        {
            static void AddEntry(BinaryWriter writer, Type keyType, Type dataType, string path)
            {
                if (keyType != null)
                {
                    string key = keyType.FullName;
                    writer.Write(key);
                }
                else
                {
                    writer.Write("-");
                }

                string data = dataType.FullName;
                writer.Write(data);

                string title = Path.GetFileName(path).Split(".")[0];
                writer.Write(title);
                byte[] bytes = File.ReadAllBytes(path);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            if (TypeConfigs == null)
            {
                return;
            }
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                foreach (var config in TypeConfigs)
                {
                    foreach (string s in config.Units)
                    {
                        if (s.Contains("."))
                        {
                            AddEntry(writer, config.KeyType, config.DataType, Path.Join(BaseDir, s));
                        }
                        else
                        {
                            string ext = config.KeyType == null ? "json" : "csv";
                            foreach (var p in Directory.EnumerateFiles(BaseDir, $"*.{s}.{ext}", SearchOption.AllDirectories))
                            {
                                AddEntry(writer, config.KeyType, config.DataType, p);
                            }
                        } 
                    }
                }
            }
        }

    }

}