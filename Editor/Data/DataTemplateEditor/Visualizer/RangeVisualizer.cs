using System;
using UnityEngine;
using UnityEditor;
using DataInspector;

namespace UnityCommonEx
{

    internal class FloatRangeVisualizer : VisualizerBase
    {

        public override bool InspectSelf(Inspector inspector, string name, ref object data, Type type)
        {
            GUITools.LabelField(name);
            return false;
        }

        public override bool InspectChildren(Inspector inspector, string path, ref object data, Type type)
        {
            FloatRange result;
            EditorGUILayout.BeginHorizontal();
            result.min = GUITools.FloatField("", ((FloatRange)data).min);
            GUILayout.Label("-", GUILayout.Width(10));
            result.max = GUITools.FloatField("", ((FloatRange)data).max);
            EditorGUILayout.EndHorizontal();
            return ApplyValueIfNotEqual(ref data, result);
        }

        public override bool HasChildren() => true;
        public override bool AlwaysShowChildren() => true;

    }

    internal class IntRangeVisualizer : VisualizerBase
    {

        public override bool InspectSelf(Inspector inspector, string name, ref object data, Type type)
        {
            GUITools.LabelField(name);
            return false;
        }

        public override bool InspectChildren(Inspector inspector, string path, ref object data, Type type)
        {
            IntRange result;
            EditorGUILayout.BeginHorizontal();
            result.min = GUITools.IntField("", ((IntRange)data).min);
            GUILayout.Label("-", GUILayout.Width(10));
            result.max = GUITools.IntField("", ((IntRange)data).max);
            EditorGUILayout.EndHorizontal();
            return ApplyValueIfNotEqual(ref data, result);
        }

        public override bool HasChildren() => true;
        public override bool AlwaysShowChildren() => true;

    }

}