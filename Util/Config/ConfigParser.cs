using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityCommonEx
{

    public static class ConfigParser
    {
        // 自定义类型转换函数字典
        private static readonly Dictionary<Type, Func<string, object>> customConverters = new Dictionary<Type, Func<string, object>>();

        /// <summary>
        /// 注册自定义类型转换函数
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="converter">转换函数（从 string 到 object）</param>
        public static void RegisterConverter(Type type, Func<string, object> converter)
        {
            if (type == null || converter == null)
            {
                LogUtil.Error("ConfigParser.RegisterConverter: type or converter cannot be null");
                return;
            }
            customConverters[type] = converter;
        }

        /// <summary>
        /// 注册自定义类型转换函数（泛型版本）
        /// </summary>
        public static void RegisterConverter<T>(Func<string, T> converter)
        {
            RegisterConverter(typeof(T), (string value) => converter(value));
        }

        public static void ParseAllConfigs(string folderPath)
        {
            var processedSections = new HashSet<string>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    LogUtil.Error($"folder does not exist: {folderPath}");
                    return;
                }

                var iniFiles = Directory.GetFiles(folderPath, "*.ini", SearchOption.AllDirectories);
                
                foreach (var filePath in iniFiles)
                {
                    try
                    {
                        ParseConfigData(IniFileParser.ParseFile(filePath), filePath, processedSections);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error($"parsing {filePath} error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error($"scanning folder error: {ex.Message}");
            }
        }

        public static void ParsePackedConfigs(string resourcePath)
        {
            TextAsset data = Resources.Load<TextAsset>(resourcePath);
            if (data == null)
            {
                LogUtil.Error("packed config resource not found: {0}", resourcePath);
                return;
            }

            var processedSections = new HashSet<string>();

            try
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(data.bytes), Encoding.UTF8))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        string entryPath = reader.ReadString();
                        int length = reader.ReadInt32();
                        string content = Encoding.UTF8.GetString(reader.ReadBytes(length));
                        ParseConfigData(IniFileParser.ParseContent(content), entryPath, processedSections);
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("parsing packed config resource {0} error: {1}", resourcePath, ex.Message);
            }
        }

        private static void ParseConfigData(
            Dictionary<string, Dictionary<string, string>> configData,
            string sourceName,
            HashSet<string> processedSections = null)
        {
            processedSections ??= new HashSet<string>();

            foreach (var section in configData)
            {
                string sectionName = section.Key;

                if (processedSections.Contains(sectionName))
                {
                    LogUtil.Error($"section {sectionName} has already been processed from another file");
                    continue;
                }

                processedSections.Add(sectionName);

                Type configClass = FindConfigClass(sectionName);
                if (configClass == null)
                {
                    LogUtil.Error($"cannot find class for section {sectionName} from {sourceName}");
                    continue;
                }

                foreach (var kvp in section.Value)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    if (!SetStaticFieldValue(configClass, key, value))
                    {
                        LogUtil.Error($"failed to set static field {key} in class {configClass.Name} with value {value} from {sourceName}");
                    }
                }
            }
        }

        private static Type FindConfigClass(string sectionName)
        {
            // Search in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private static bool SetStaticFieldValue(Type configClass, string fieldName, string value)
        {
            try
            {
                // Find the static field by name
                FieldInfo field = configClass.GetField(fieldName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                
                if (field == null)
                {
                    LogUtil.Error($"static field {fieldName} not found in class {configClass.Name}");
                    return false;
                }
                
                // Check if field is static
                if (!field.IsStatic)
                {
                    LogUtil.Error($"field {fieldName} in class {configClass.Name} is not static");
                    return false;
                }
                
                // Convert string value to field type and set it
                object convertedValue = ConvertValue(value, field.FieldType);
                if (convertedValue != null)
                {
                    field.SetValue(null, convertedValue);
                    return true;
                }
                else
                {
                    LogUtil.Error($"failed to convert value {value} to type {field.FieldType.Name} for field {fieldName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error($"error setting static field {fieldName} in class {configClass.Name}: {ex.Message}");
                return false;
            }
        }

        private static object ConvertValue(string value, Type targetType)
        {
            try
            {
                // 首先检查是否有自定义转换器
                if (customConverters.TryGetValue(targetType, out var customConverter))
                {
                    return customConverter(value);
                }

                if (targetType == typeof(string))
                {
                    return value;
                }
                else if (targetType == typeof(int))
                {
                    return int.Parse(value);
                }
                else if (targetType == typeof(float))
                {
                    return float.Parse(value);
                }
                else if (targetType == typeof(double))
                {
                    return double.Parse(value);
                }
                else if (targetType == typeof(bool))
                {
                    return bool.Parse(value);
                }
                else if (targetType == typeof(long))
                {
                    return long.Parse(value);
                }
                else if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, value, true);
                }
                else if (targetType == typeof(Vector2))
                {
                    return ParseVector2(value);
                }
                else if (targetType == typeof(Vector3))
                {
                    return ParseVector3(value);
                }
                else if (targetType == typeof(Color))
                {
                    return ParseColor(value);
                }
                else if (targetType == typeof(FloatRange))
                {
                    return FloatRange.FromString(value);
                }
                else if (targetType.IsArray)
                {
                    return ParseArray(value, targetType);
                }
                else
                {
                    LogUtil.Error($"unsupported type {targetType.Name} for value conversion");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error($"conversion error for value {value} to type {targetType.Name}: {ex.Message}");
                return null;
            }
        }

        private static Vector2 ParseVector2(string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length != 2)
            {
                throw new FormatException($"Vector2 requires 2 values separated by comma, got {parts.Length}");
            }
            
            float x = float.Parse(parts[0].Trim());
            float y = float.Parse(parts[1].Trim());
            return new Vector2(x, y);
        }

        private static Vector3 ParseVector3(string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException($"Vector3 requires 3 values separated by comma, got {parts.Length}");
            }
            
            float x = float.Parse(parts[0].Trim());
            float y = float.Parse(parts[1].Trim());
            float z = float.Parse(parts[2].Trim());
            return new Vector3(x, y, z);
        }

        private static Color ParseColor(string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length == 3)
            {
                // RGB format, alpha defaults to 1
                float r = float.Parse(parts[0].Trim());
                float g = float.Parse(parts[1].Trim());
                float b = float.Parse(parts[2].Trim());
                return new Color(r, g, b, 1f);
            }
            else if (parts.Length == 4)
            {
                // RGBA format
                float r = float.Parse(parts[0].Trim());
                float g = float.Parse(parts[1].Trim());
                float b = float.Parse(parts[2].Trim());
                float a = float.Parse(parts[3].Trim());
                return new Color(r, g, b, a);
            }
            else
            {
                throw new FormatException($"Color requires 3 (RGB) or 4 (RGBA) values separated by comma, got {parts.Length}");
            }
        }

        private static object ParseArray(string value, Type arrayType)
        {
            Type elementType = arrayType.GetElementType();
            string[] parts = value.Split(',');
            
            Array array = Array.CreateInstance(elementType, parts.Length);
            
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmedValue = parts[i].Trim();
                object elementValue = ConvertValue(trimmedValue, elementType);
                if (elementValue != null)
                {
                    array.SetValue(elementValue, i);
                }
                else
                {
                    throw new FormatException($"failed to convert array element {i} ({trimmedValue}) to type {elementType.Name}");
                }
            }
            
            return array;
        }

    }
}
