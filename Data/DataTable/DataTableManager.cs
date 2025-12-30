using System;
using System.Collections.Generic;
using System.IO;

namespace UnityCommonEx
{

    public static class DataTableManager
    {

        public static void Validate<TK, TR>() where TR : BaseDataTableRow<TK>, new()
        {
            var list = GetAll<TK, TR>();
            if (list != null) 
            {
                foreach (var template in list)
                {
                    string msg = template.Validate();
                    if (msg != null)
                    {
                        LogUtil.Error("DataTable {0} {1} Error: {2}", typeof(TR), template.Id, msg);
                        return;
                    }
                }
            }
        }

        public static DataTable<TK, TR> Get<TK, TR>(string id) where TR : BaseDataTableRow<TK>, new()
        {
            return DataTableManagerInstance<TK, TR>.Get(id);
        }

        public static DataTable<TK, TR> Get<TK, TR>() where TR : BaseDataTableRow<TK>, new()
        {
            return DataTableManagerInstance<TK, TR>.Get();
        }

        public static TR GetRow<TK, TR>(TK key) where TR : BaseDataTableRow<TK>, new()
        {
            return DataTableManagerInstance<TK, TR>.Get().GetRow(key);
        }

        public static ICollection<DataTable<TK, TR>> GetAll<TK, TR>() where TR : BaseDataTableRow<TK>, new()
        {
            return DataTableManagerInstance<TK, TR>.GetAll();
        }

        public static void LoadSingleRaw<TK, TR>(string title, string content) where TR : BaseDataTableRow<TK>, new()
        {
            DataTableManagerInstance<TK, TR>.LoadSingleRaw(title, content);
        }

        public static void LoadSingle<TK, TR>(string path) where TR : BaseDataTableRow<TK>, new()
        {
            DataTableManagerInstance<TK, TR>.LoadSingle(path);
        }

        public static void LoadExt<TK, TR>(string path, string ext) where TR : BaseDataTableRow<TK>, new()
        {
            DataTableManagerInstance<TK, TR>.LoadExt(path, ext);
        }

        public static void Clear<TK, TR>() where TR : BaseDataTableRow<TK>, new()
        {
            DataTableManagerInstance<TK, TR>.Clear();
        }

    }

    public static class DataTableManagerInstance<TK, TR> where TR: BaseDataTableRow<TK>, new()
    {

        static Dictionary<string, DataTable<TK, TR>> Entries;
        static DataTable<TK, TR> DefaultEntry;

        public static void Clear()
        {
            if (Entries != null)
            {
                Entries.Clear();
            }
            DefaultEntry = null;
        }

        public static void LoadSingleRaw(string title, string content)
        {
            if (Entries == null)
            {
                Entries = new Dictionary<string, DataTable<TK, TR>>(StringComparer.OrdinalIgnoreCase);
            }
            DataTable<TK, TR> table = DataTable.CreateRaw<TK, TR>(title, content);
            table.Id = title;
            
            if (Entries.ContainsKey(table.Id))
            {
                LogUtil.Warn("already load table of id {0}", table.Id);
                Entries[table.Id] = table;
            }
            else {
                Entries.Add(table.Id, table);
            }
            DefaultEntry = table;
        }

        public static void LoadSingle(string path)
        {
            if (Entries == null)
            {
                Entries = new Dictionary<string, DataTable<TK, TR>>(StringComparer.OrdinalIgnoreCase);
            }
            DataTable<TK, TR> table = DataTable.Create<TK, TR>(path);
            table.Id = Path.GetFileName(path).Split(".")[0];
            
            if (Entries.ContainsKey(table.Id))
            {
                LogUtil.Warn("already load table of id {0}", table.Id);
                Entries[table.Id] = table;
            }
            else {
                Entries.Add(table.Id, table);
            }
            DefaultEntry = table;
        }

        public static void LoadExt(string path, string ext)
        {
            foreach (var p in Directory.EnumerateFiles(path, $"*.{ext}.csv", SearchOption.AllDirectories))
            {
                LoadSingle(p);
            }
        }

        public static DataTable<TK, TR> Get()
        {
            return DefaultEntry;    
        }

        public static DataTable<TK, TR> Get(string id)
        {
            return Entries?.GetValueOrDefault(id, null);
        }

        public static ICollection<DataTable<TK, TR>> GetAll()
        {
            return Entries?.Values;
        }

    }

}