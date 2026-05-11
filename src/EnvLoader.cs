using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LessonVersion
{
    public static class EnvLoader
    {
        public static void Load(string filePath = ".env")
        {
            if (!File.Exists(filePath))
                return;

            var lines = File.ReadAllLines(filePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("#"))
                    continue;
                
                var separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = trimmedLine.Substring(0, separatorIndex).Trim();
                var value = trimmedLine.Substring(separatorIndex + 1).Trim();
                
                if (value.Length >= 2 && 
                    ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                     (value.StartsWith("'") && value.EndsWith("'"))))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                
                Environment.SetEnvironmentVariable(key, value);
            }
        }
        
        public static string GetString(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }
        
        public static int GetInt(string key, int defaultValue = 0)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
        
        public static bool GetBool(string key, bool defaultValue = false)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }
    }
}