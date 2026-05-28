using System;
using System.Collections.Generic;

namespace UnityCommonEx
{
    public class StringTableRow : BaseDataTableRow<string>
    {
        public string Key;
        public MultiLingualText Value;

        public override string RowKey => Key ?? string.Empty;
    }

    public static class StringTable
    {
        private const string TableId = "StringTable";
        private static readonly HashSet<string> WarnedMissingKeys = new HashSet<string>();

        public static string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                WarnMissing(key);
                return fallback ?? string.Empty;
            }

            var table = DataTableManager.Get<string, StringTableRow>(TableId)
                ?? DataTableManager.Get<string, StringTableRow>();
            if (table == null)
            {
                WarnMissing(key, "table not loaded");
                return fallback ?? string.Empty;
            }

            var row = table.GetRow(key);
            if (row?.Value == null)
            {
                WarnMissing(key);
                return fallback ?? string.Empty;
            }

            string text = NormalizeEscapes(row.Value.GetText());
            if (string.IsNullOrEmpty(text))
            {
                WarnMissing(key, "value empty");
                return fallback ?? string.Empty;
            }

            return text;
        }

        public static bool TryGet(string key, out string text)
        {
            text = Get(key, null);
            return !string.IsNullOrEmpty(text);
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            string format = Get(key, fallback);
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            try
            {
                return string.Format(format, args);
            }
            catch (Exception e)
            {
                LogUtil.Warn("StringTable: format failed for key {0}: {1}", key, e.Message);
                return format;
            }
        }

        private static void WarnMissing(string key, string reason = "not found")
        {
            string warnKey = key + "|" + reason;
            if (!WarnedMissingKeys.Add(warnKey))
                return;
            LogUtil.Warn("StringTable: key '{0}' {1}", key, reason);
        }

        private static string NormalizeEscapes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }
    }
}
