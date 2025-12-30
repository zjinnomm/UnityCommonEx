using System;
using UnityEditor;
using UnityEngine;

namespace UnityCommonEx
{
    [CustomPropertyDrawer(typeof(UITweenEntry))]
    public class UITweenEntryDrawer : PropertyDrawer
    {

        const float UnitPropHeight = 18;
        const float UnitPropMargin = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            position.height = UnitPropHeight;
            
            var propType = property.FindPropertyRelative("PropType");
            propType.enumValueFlag = Convert.ToInt32(EditorGUI.EnumFlagsField(position, "Tween Props", (UITweenPropType) propType.enumValueFlag));
            position.y += UnitPropHeight + UnitPropMargin;

            if ((propType.enumValueFlag & (byte)UITweenPropType.Position) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("Position"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.Rotation) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("Rotation"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.Scale) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("Scale"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.Alpha) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("Alpha"));
                position.y += UnitPropHeight + UnitPropMargin;
            }

            EditorGUI.PropertyField(position, property.FindPropertyRelative("IsRelative"));
            position.y += UnitPropHeight + UnitPropMargin;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Duration"));
            position.y += UnitPropHeight + UnitPropMargin;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("CurveType"));
            position.y += UnitPropHeight + UnitPropMargin;
            
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int prop = 4;
            int propType = property.FindPropertyRelative("PropType").enumValueFlag;
            if ((propType & (byte)UITweenPropType.Position) > 0)
            {
                prop ++;
            }
            if ((propType & (byte)UITweenPropType.Rotation) > 0)
            {
                prop ++;
            }
            if ((propType & (byte)UITweenPropType.Scale) > 0)
            {
                prop ++;
            }
            if ((propType & (byte)UITweenPropType.Alpha) > 0)
            {
                prop ++;
            }
            return prop * (UnitPropHeight + UnitPropMargin) - UnitPropMargin;
        }

    }

}