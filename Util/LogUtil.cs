using UnityEngine;
using System;
using System.IO;

namespace UnityCommonEx
{

    public static class LogUtil
    {
        static string logDirectory;

        public static void Info(string msg)
        {
            Debug.Log(msg);
        }

        public static void Info(string msg, params object[] args)
        {
            Debug.LogFormat(msg, args);
        }

        public static void Error(string msg)
        {
            Debug.LogError(msg);
            throw new Exception(msg);
        }

        public static void Error(string msg, params object[] args)
        {
            Debug.LogErrorFormat(msg, args);
            throw new Exception(string.Format(msg, args));
        }

        public static void Warn(string msg)
        {
            Debug.LogWarning(msg);
        }

        public static void Warn(string msg, params object[] args)
        {
            Debug.LogWarningFormat(msg, args);
        }

        static FileStream stream;
        static StreamWriter writer;

        public static void Init(string directoryPath = null)
        {
            logDirectory = string.IsNullOrEmpty(directoryPath)
                ? Path.Combine(Application.persistentDataPath, "Logs")
                : directoryPath;

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            string logPath = Path.Combine(logDirectory, $"{DateTime.Now.ToLocalTime():yyyy-MM-dd HH-mm-ss}.log");
            stream = new FileStream(logPath, FileMode.OpenOrCreate);
            writer = new StreamWriter(stream);
            Application.logMessageReceived += HandleLog;
        }

        public static void Release()
        {
            Application.logMessageReceived -= HandleLog;
            writer?.Close();
            stream?.Close();
            writer = null;
            stream = null;
        }

        static void HandleLog(string logString, string stackTrace, LogType type)
        {
            writer.WriteLine($"[{Time.time}s {Time.frameCount}f][{type}]: {logString}");
            if (type == LogType.Error || type == LogType.Warning || type == LogType.Exception) 
            {
                writer.WriteLine(stackTrace);
            }
            writer.Flush();
        }

    }

}
