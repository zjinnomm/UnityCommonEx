using System;
using UnityEditor;
using UnityEngine;

namespace UnityCommonEx
{
    [CustomPropertyDrawer(typeof(UITweenConfig))]
    public class UITweenConfigDrawer : PropertyDrawer
    {

        const float UnitPropHeight = 18;
        const float UnitPropMargin = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            position.height = UnitPropHeight;
            
            var propType = property.FindPropertyRelative("PropType");
            propType.enumValueFlag = Convert.ToInt32(EditorGUI.EnumFlagsField(position, "Tween Props", (UITweenPropType) propType.enumValueFlag));
            position.y += UnitPropHeight + UnitPropMargin;
            
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Duration"));
            position.y += UnitPropHeight + UnitPropMargin;

            if ((propType.enumValueFlag & (byte)UITweenPropType.Position) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinPos"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxPos"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("PosXCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("PosYCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.Alpha) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinAlpha"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxAlpha"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("AlphaCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.SizeX) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinSizeX"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxSizeX"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("SizeXCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.SizeY) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinSizeY"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxSizeY"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("SizeYCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            if ((propType.enumValueFlag & (byte)UITweenPropType.Scale) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinScale"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxScale"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("ScaleCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }
            
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int prop = 2; // PropType, Duration
            int propType = property.FindPropertyRelative("PropType").enumValueFlag;
            if ((propType & (byte)UITweenPropType.Position) > 0)
            {
                prop += 4; // MinPos, MaxPos, PosXCurve, PosYCurve
            }
            if ((propType & (byte)UITweenPropType.Alpha) > 0)
            {
                prop += 3; // MinAlpha, MaxAlpha, AlphaCurve
            }
            if ((propType & (byte)UITweenPropType.SizeX) > 0)
            {
                prop += 3; // MinSizeX, MaxSizeX, SizeXCurve
            }
            if ((propType & (byte)UITweenPropType.SizeY) > 0)
            {
                prop += 3; // MinSizeY, MaxSizeY, SizeYCurve
            }
            if ((propType & (byte)UITweenPropType.Scale) > 0)
            {
                prop += 3; // MinScale, MaxScale, ScaleCurve
            }
            return prop * (UnitPropHeight + UnitPropMargin) - UnitPropMargin;
        }

    }

}
