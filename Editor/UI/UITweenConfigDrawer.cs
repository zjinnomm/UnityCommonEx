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
        const float FoldoutWidth = 18;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            position.height = UnitPropHeight;

            var propType = property.FindPropertyRelative("PropType");
            Rect propTypeRect = new Rect(position.x, position.y, position.width - FoldoutWidth - UnitPropMargin, position.height);
            Rect foldoutRect = new Rect(propTypeRect.xMax + UnitPropMargin, position.y, FoldoutWidth, position.height);

            propType.enumValueFlag = Convert.ToInt32(EditorGUI.EnumFlagsField(propTypeRect, "Type Mask", (UITweenPropType)propType.enumValueFlag));
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

            if (!property.isExpanded)
            {
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUI.EndProperty();
                return;
            }

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
            if ((propType.enumValueFlag & (byte)UITweenPropType.Rotation) > 0)
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MinRot"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxRot"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("RotXCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("RotYCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("RotZCurve"));
                position.y += UnitPropHeight + UnitPropMargin;
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return UnitPropHeight;

            int prop = 2;
            int propType = property.FindPropertyRelative("PropType").enumValueFlag;
            if ((propType & (byte)UITweenPropType.Position) > 0)
                prop += 4;
            if ((propType & (byte)UITweenPropType.Alpha) > 0)
                prop += 3;
            if ((propType & (byte)UITweenPropType.SizeX) > 0)
                prop += 3;
            if ((propType & (byte)UITweenPropType.SizeY) > 0)
                prop += 3;
            if ((propType & (byte)UITweenPropType.Scale) > 0)
                prop += 3;
            if ((propType & (byte)UITweenPropType.Rotation) > 0)
                prop += 5;
            return prop * (UnitPropHeight + UnitPropMargin) - UnitPropMargin;
        }

    }

}
