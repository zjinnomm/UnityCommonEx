using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityCommonEx
{

    public interface IDataTableReference : IDataTableStringField
    {
        public string Id { set; get; }

    }

    [Serializable]
    public struct DataTableReference<TK, TR> : IDataTableReference where TR : BaseDataTableRow<TK>, new()
    {

        private string id;

        public DataTable<TK, TR> Data
        {
            get
            {
                if (cachedResolved == null || cachedResolved.Id != id)
                {
                    cachedResolved = DataTableManager.Get<TK, TR>(id);
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
                cachedResolved = DataTableManager.Get<TK, TR>(id);
                return cachedResolved != null;
            }
        }

        [NonSerialized]
        DataTable<TK, TR> cachedResolved;

        public void SetFromString(string value)
        {
            Id = value;
        }

    }

    public class DataTableReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDataTableReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object result = Activator.CreateInstance(objectType);
            ((IDataTableReference)result).Id = (string)reader.Value;
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((IDataTableReference)value).Id);
        }
    }

    public interface IDataTableRowReference<TK> : IDataTableRowReference
    {
        public string TableId { get; set; }
        public TK RowId { get; set; }
    }

    public interface IDataTableRowReference : IDataTableStringField
    {

    }

    [Serializable]
    public struct DataTableRowReference<TK, TR> : IDataTableRowReference<TK> where TR : BaseDataTableRow<TK>, new()
    {
        private string tableId;
        private TK rowId;

        public string TableId
        {
            get => tableId;
            set
            {
                if (value != tableId)
                {
                    tableId = value;
                    cachedResolved = null;
                }
            }
        }

        public TK RowId
        {
            get => rowId;
            set
            {
                if (!EqualityComparer<TK>.Default.Equals(value, rowId))
                {
                    rowId = value;
                    cachedResolved = null;
                }
            }
        }

        public TR Data
        {
            get
            {
                if (cachedResolved == null || (tableId != null && cachedResolvedTable?.Id != tableId) || (cachedResolved != null && !EqualityComparer<TK>.Default.Equals(cachedResolved.RowKey, rowId)))
                {
                    cachedResolved = ResolveRow();
                }
                return cachedResolved;
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
                if (EqualityComparer<TK>.Default.Equals(rowId, default(TK)))
                {
                    return false;
                }
                cachedResolved = ResolveRow();
                return cachedResolved != null;
            }
        }

        private TR ResolveRow()
        {
            // 检查rowId是否为默认值（对于值类型）或null（对于引用类型）
            if (EqualityComparer<TK>.Default.Equals(rowId, default(TK)))
            {
                return null;
            }

            // 如果TableId为null，获取默认表
            cachedResolvedTable = string.IsNullOrEmpty(tableId) 
                ? DataTableManager.Get<TK, TR>() 
                : DataTableManager.Get<TK, TR>(tableId);

            if (cachedResolvedTable == null)
            {
                return null;
            }

            // rowId已经是TK类型，直接使用
            return cachedResolvedTable.GetRow(rowId);
        }

        [NonSerialized]
        TR cachedResolved;
        DataTable<TK, TR> cachedResolvedTable;

        public void SetFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                TableId = null;
                RowId = default;
                return;
            }

            if (input.Contains(":"))
            {
                string[] parts = input.Split(':', 2);
                TableId = parts[0];
                RowId = (TK)ConvertStringToType(parts[1], typeof(TK));
                return;
            }

            TableId = null;
            RowId = (TK)ConvertStringToType(input, typeof(TK));
        }

        static object ConvertStringToType(string input, Type targetType)
        {
            if (targetType == typeof(int))
                return int.Parse(input);
            if (targetType == typeof(string))
                return input;
            if (targetType == typeof(long))
                return long.Parse(input);
            throw new NotSupportedException($"Unsupported type: {targetType}");
        }
    }

    public class DataTableRowReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDataTableRowReference).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string input = (string)reader.Value;
            object result = Activator.CreateInstance(objectType);
            
            if (string.IsNullOrEmpty(input))
            {
                return result;
            }

            // 检查是否包含":"字符
            if (input.Contains(":"))
            {
                string[] parts = input.Split(':', 2); // 最多分割2次
                string tableId = parts[0];
                string rowIdStr = parts[1];
                
                // 设置TableId
                var tableIdProperty = objectType.GetProperty("TableId");
                tableIdProperty.SetValue(result, tableId);
                
                // 设置RowId（需要转换为正确的类型）
                var rowIdProperty = objectType.GetProperty("RowId");
                var rowIdType = rowIdProperty.PropertyType;
                object convertedRowId = ConvertStringToType(rowIdStr, rowIdType);
                rowIdProperty.SetValue(result, convertedRowId);
            }
            else
            {
                // 整个字符串都是RowId
                var tableIdProperty = objectType.GetProperty("TableId");
                tableIdProperty.SetValue(result, null);
                
                var rowIdProperty = objectType.GetProperty("RowId");
                var rowIdType = rowIdProperty.PropertyType;
                object convertedRowId = ConvertStringToType(input, rowIdType);
                rowIdProperty.SetValue(result, convertedRowId);
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var tableIdProperty = value.GetType().GetProperty("TableId");
            var rowIdProperty = value.GetType().GetProperty("RowId");
            
            string tableId = (string)tableIdProperty.GetValue(value);
            object rowId = rowIdProperty.GetValue(value);
            
            if (string.IsNullOrEmpty(tableId))
            {
                writer.WriteValue(rowId.ToString());
            }
            else
            {
                writer.WriteValue($"{tableId}:{rowId}");
            }
        }

        private object ConvertStringToType(string input, Type targetType)
        {
            if (targetType == typeof(int))
            {
                return int.Parse(input);
            }
            else if (targetType == typeof(string))
            {
                return input;
            }
            else if (targetType == typeof(long))
            {
                return long.Parse(input);
            }
            else
            {
                throw new NotSupportedException($"Unsupported type: {targetType}");
            }
        }
    }

}
