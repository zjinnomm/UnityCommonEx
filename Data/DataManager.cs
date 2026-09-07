using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityCommonEx
{

    [Serializable]
    public abstract class BaseData { }
    
    public static class DataManager
    {

        public static void LoadScattered(string path)
        {
            var config = JsonUtil.Read<ScatteredDataLoadConfig>(path);
            config.Prepare();
            config.Load();
        }

        public static void LoadPacked(string path)
        {
            TextAsset data = Resources.Load(path) as TextAsset;
            LoadPacked(data.bytes);
        }

        public static void LoadPacked(byte[] bytes)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    Type keyType = Type.GetType(reader.ReadString());
                    Type dataType = Type.GetType(reader.ReadString());
                    string title = reader.ReadString();
                    int length = reader.ReadInt32();
                    string content = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
                    LoadSingleRaw(keyType, dataType, title, content);
                }
            }
        }

        public static void Clear(Type keyType, Type dataType)
        {
            if (keyType != null)
            {
                typeof(DataTableManager).GetMethod("Clear").MakeGenericMethod(keyType, dataType).InvokeOptimized(null);
            }
            else
            {
                typeof(DataTemplateManager).GetMethod("Clear").MakeGenericMethod(dataType).InvokeOptimized(null);
            }
        }

        public static void Validate(Type keyType, Type dataType)
        {
            if (keyType != null)
            {
                typeof(DataTableManager).GetMethod("Validate").MakeGenericMethod(keyType, dataType).InvokeOptimized(null);
            }
            else
            {
                typeof(DataTemplateManager).GetMethod("Validate").MakeGenericMethod(dataType).InvokeOptimized(null);
            }
        }

        public static void LoadByPath(Type keyType, Type dataType, string path)
        {
            if (keyType != null)
            {
                typeof(DataTableManager).GetMethod("LoadSingle").MakeGenericMethod(keyType, dataType).InvokeOptimized(null, path);
            }
            else
            {
                typeof(DataTemplateManager).GetMethod("LoadSingle").MakeGenericMethod(dataType).InvokeOptimized(null, path);
            }
        }

        public static void LoadByExt(Type keyType, Type dataType, string path, string ext)
        {
            if (keyType != null)
            {
                typeof(DataTableManager).GetMethod("LoadExt").MakeGenericMethod(keyType, dataType).InvokeOptimized(null, path, ext);
            }
            else
            {
                typeof(DataTemplateManager).GetMethod("LoadExt").MakeGenericMethod(dataType).InvokeOptimized(null, path, ext);
            }
        }

        public static void LoadSingleRaw(Type keyType, Type dataType, string title, string content)
        {
            if (keyType != null)
            {
                typeof(DataTableManager).GetMethod("LoadSingleRaw").MakeGenericMethod(keyType, dataType).InvokeOptimized(null, title, content);
            }
            else
            {
                typeof(DataTemplateManager).GetMethod("LoadSingleRaw").MakeGenericMethod(dataType).InvokeOptimized(null, title, content);
            }
        }

    }

}