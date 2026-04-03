using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;

namespace UnityCommonEx
{

    public abstract class DataTable
    {

        public string Id;
        public abstract string Validate();

        public static DataTable<TK, TR> CreateRaw<TK, TR>(string title, string content) where TR : BaseDataTableRow<TK>, new()
        {
            DataTable<TK, TR> result = new DataTable<TK, TR>();
            result.LoadRaw(title, content);
            return result;
        }

        public static DataTable<TK, TR> Create<TK, TR>(string path) where TR : BaseDataTableRow<TK>, new()
        {
            DataTable<TK, TR> result = new DataTable<TK, TR>();
            result.Load(path);
            return result;
        }

    }

    public class DataTable<TK, TR> : DataTable where TR : BaseDataTableRow<TK>, new()
    {

        readonly List<TR> rows = new List<TR>();
        readonly Dictionary<TK, TR> rowDict = new Dictionary<TK, TR>();

        public int Count => rows.Count;

        public TR GetRow(TK rowKey)
        {
            return rowDict.GetValueOrDefault(rowKey, null);
        }

        public TR this[int index] => (index >= 0 && index < rows.Count) ? rows[index] : null;

        void Load(IList<string[]> content, string context)
        {
            rows.Clear();
            if (content.Count == 0)
            {
                return;
            }

            string[] titles = content[0];
            FieldInfo[] fields = typeof(TR).GetFields();
            Action<string, TR>[] fieldActions = new Action<string, TR>[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                string title = titles[i];
                string titleUpper = title.ToUpper();
                if (title.Contains('.'))
                {
                    string[] titleSplit = title.Split('.');
                    string mainTitle = titleSplit[0].ToUpper();
                    string subTitle = title.Substring(mainTitle.Length + 1);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        FieldInfo field = fields[j];
                        if (field.Name.ToUpper() == mainTitle)
                        {
                            // 优先处理数组类型
                            if (field.FieldType.IsArray)
                            {
                                Type elementType = field.FieldType.GetElementType();
                                var elementResolver = StringTypeResolver.GetResolver(elementType);
                                if (int.TryParse(subTitle, out int arrayIndex))
                                {
                                    fieldActions[i] = (s, row) => 
                                    {
                                        Array array = field.GetValue(row) as Array;
                                        if (array == null)
                                        {
                                            array = Array.CreateInstance(elementType, arrayIndex + 1);
                                            field.SetValueOptimized(row, array);
                                        }
                                        if (arrayIndex >= array.Length)
                                        {
                                            Array newArray = Array.CreateInstance(elementType, arrayIndex + 1);
                                            Array.Copy(array, newArray, array.Length);
                                            field.SetValueOptimized(row, newArray);
                                            array = newArray;
                                        }
                                        array.SetValue(elementResolver.Invoke(s), arrayIndex);
                                    };
                                }
                            }
                            // 其次处理泛型IList/IDictionary
                            else if (field.FieldType.IsGenericType && typeof(IDictionary).IsAssignableFrom(field.FieldType))
                            {
                                Type keyType = field.FieldType.GetGenericArguments()[0];
                                Type valueType = field.FieldType.GetGenericArguments()[1];
                                object key = StringTypeResolver.GetResolver(keyType).Invoke(subTitle);
                                var valueResolver = StringTypeResolver.GetResolver(valueType);
                                fieldActions[i] = (s, row) =>
                                {
                                    IDictionary dict = field.GetValue(row) as IDictionary;
                                    if (dict == null)
                                    {
                                        dict = Activator.CreateInstance(field.FieldType) as IDictionary;
                                        field.SetValueOptimized(row, dict);
                                    }
                                    dict.Add(key, valueResolver.Invoke(s));
                                };
                            }
                            else if (field.FieldType.IsGenericType && typeof(IList).IsAssignableFrom(field.FieldType))
                            {
                                Type elementType = field.FieldType.GetGenericArguments()[0];
                                var elementResolver = StringTypeResolver.GetResolver(elementType);
                                fieldActions[i] = (s, row) => 
                                {
                                    IList list = field.GetValue(row) as IList;
                                    if (list == null)
                                    {
                                        list = Activator.CreateInstance(field.FieldType) as IList;
                                        field.SetValueOptimized(row, list);
                                    }
                                    list.Add(elementResolver.Invoke(s));
                                };
                            }
                            else if (field.FieldType == typeof(MultiLingualText))
                            {
                                if (Enum.TryParse<LanguageType>(subTitle, true, out var langType))
                                {
                                    fieldActions[i] = (s, row) =>
                                    {
                                        var ml = field.GetValue(row) as MultiLingualText;
                                        if (ml == null)
                                        {
                                            ml = new MultiLingualText();
                                            field.SetValueOptimized(row, ml);
                                        }
                                        ml._map[langType] = s;
                                    };
                                }
                            }
                            break;
                        }
                    }
                }
                else 
                {
                    for (int j = 0; j < fields.Length; j++)
                    {
                        FieldInfo field = fields[j];
                        if (field.Name.ToUpper() == titleUpper)
                        {
                            var resolver = StringTypeResolver.GetResolver(field.FieldType);
                            fieldActions[i] = (s, row) => field.SetValueOptimized(row, resolver.Invoke(s));
                            break;
                        }
                    }
                }
                if (fieldActions[i] == null)
                {
                    LogUtil.Warn("{0}: field {1} finds no match.", context, title);
                }
            }

            for (int i = 1; i < content.Count; i++)
            {
                string[] values = content[i];
                if (values.Length == 0 || (values.Length == 1 && string.IsNullOrEmpty(values[0])))
                {
                    continue;
                }
                TR row = new TR();
                for (int j = 0; j < values.Length && j < titles.Length; j++)
                {
                    Action<string, TR> fieldAction = fieldActions[j];
                    string s = values[j];
                    if (s.Length > 0)
                    {
                        fieldAction?.Invoke(s, row);
                    }
                }
                if (rowDict.ContainsKey(row.RowKey))
                {
                    LogUtil.Error("table {0} contains duplicate row {1}", context, row.RowKey);
                    continue;
                }
                row.PostInit();
                rows.Add(row);
                rowDict.Add(row.RowKey, row);
            }
        }

        public void LoadRaw(string title, string content)
        {
            Load(CsvUtil.ReadCsvRaw(content), title);
        }

        public void Load(string path)
        {
            Load(CsvUtil.ReadCsv(path), path);
        }

        public IEnumerator<KeyValuePair<TK, TR>> GetEnumerator()
        {
            foreach (var kvp in rowDict)
            {
                yield return kvp;
            }
        }

        public override string Validate()
        {
            foreach (var row in rows)
            {
                string msg = row.Validate();
                if (msg != null)
                {
                    return $"{row.RowKey}: {msg}" ;
                }
            }
            return null;
        }
    }

}