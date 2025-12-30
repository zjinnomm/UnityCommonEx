using UnityEngine;
using System;
using System.IO;

namespace UnityCommonEx
{

    public static class LogUtil
    {

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

        public static void Init()
        {
            if (!Directory.Exists("log"))
                Directory.CreateDirectory("log");
            stream = new FileStream($"log/{DateTime.Now.ToLocalTime():yyyy-MM-dd HH-mm-ss}.log", FileMode.OpenOrCreate);
            writer = new StreamWriter(stream);
            Application.logMessageReceived += HandleLog;
        }

        public static void Release()
        {
            writer.Close();
            stream.Close();
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