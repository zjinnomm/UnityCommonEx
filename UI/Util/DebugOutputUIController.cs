using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace UnityCommonEx
{

    public class DebugOutputUIController : SingletonController<DebugOutputUIController>
    {

        public static void TryAppendLine(string str)
        {
            if (Instance != null && Instance.Activated)
            {
                Instance.AppendLine(str);
            }
        }

        struct DebugOutputLine
        {

            public string Line;
            public float Time;

        }

        public TMP_Text Text;
        public int MaxOutputLines = 40;
        public float LineKeepDuration = 5;
        public bool OutputToLog = false;

        bool needsRefresh = false;
        StringBuilder sb = new StringBuilder();
        Queue<DebugOutputLine> outputLines = new Queue<DebugOutputLine>();

        public void Clear()
        {
            if (outputLines.Count > 0)
            {
                outputLines.Clear();
                needsRefresh = true;
            }
        }

        public void AppendLine(string str)
        {
            if (OutputToLog)
            {
                LogUtil.Info(str);
            }
            if (outputLines.Count >= MaxOutputLines)
            {
                outputLines.Dequeue();
            }
            DebugOutputLine line = new DebugOutputLine { Line = str, Time = Time.realtimeSinceStartup };
            outputLines.Enqueue(line);
            needsRefresh = true;
        }

        private void Update()
        {
            float timeout = Time.realtimeSinceStartup - LineKeepDuration;
            while (outputLines.Count > 0 && outputLines.Peek().Time <= timeout)
            {
                outputLines.Dequeue();
                needsRefresh = true;
            }

            if (needsRefresh)
            {
                needsRefresh = false;
                foreach (DebugOutputLine line in outputLines)
                {
                    sb.AppendLine($"<color=green>[{line.Time:f2}] <color=white>{line.Line}");
                }
                Text.text = sb.ToString();
                sb.Clear();
            }
        }

        private void OnDisable()
        {
            Clear();
        }

    }
}