using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityCommonEx
{
    
    public static class IniFileParser
    {

        public static Dictionary<string, Dictionary<string, string>> ParseContent(string content)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            
            if (string.IsNullOrEmpty(content))
                return result;

            string currentSection = "";
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    if (!result.ContainsKey(currentSection))
                    {
                        result[currentSection] = new Dictionary<string, string>();
                    }
                    continue;
                }

                var equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = trimmedLine.Substring(0, equalIndex).Trim();
                    var value = trimmedLine.Substring(equalIndex + 1).Trim();
                    
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (string.IsNullOrEmpty(currentSection))
                    {
                        currentSection = "Default";
                        if (!result.ContainsKey(currentSection))
                        {
                            result[currentSection] = new Dictionary<string, string>();
                        }
                    }

                    result[currentSection][key] = value;
                }
            }

            return result;
        }

        public static Dictionary<string, Dictionary<string, string>> ParseFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"ini file does not exist: {filePath}");
                    return new Dictionary<string, Dictionary<string, string>>();
                }

                var content = File.ReadAllText(filePath, Encoding.UTF8);
                return ParseContent(content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"parse ini file error: {ex.Message}");
                return new Dictionary<string, Dictionary<string, string>>();
            }
        }

    }
}
