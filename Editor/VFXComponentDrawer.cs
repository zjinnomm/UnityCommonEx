using UnityEngine;
using UnityEditor;

namespace UnityCommonEx
{
    [CustomPropertyDrawer(typeof(VFXComponent))]
    public class VFXComponentDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 获取component字段
            SerializedProperty componentProp = property.FindPropertyRelative("component");

            // 创建对象字段，但只允许特定类型
            Component currentComponent = componentProp.objectReferenceValue as Component;
            
            // 绘制标签
            Rect labelRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
            EditorGUI.LabelField(labelRect, label);

            // 绘制对象字段，允许拖拽GameObject或Component
            Rect fieldRect = new Rect(position.x + position.width * 0.3f, position.y, position.width * 0.4f, position.height);
            Object newObject = EditorGUI.ObjectField(fieldRect, currentComponent, typeof(Object), true);
            
            // 处理拖拽的对象
            if (newObject != null && newObject != currentComponent)
            {
                Component newComponent = null;
                
                if (newObject is Component)
                {
                    // 直接拖拽的是Component
                    newComponent = newObject as Component;
                }
                else if (newObject is GameObject)
                {
                    // 拖拽的是GameObject，尝试找到合法的组件
                    GameObject go = newObject as GameObject;
                    newComponent = FindValidComponent(go);
                    
                    if (newComponent == null)
                    {
                        EditorUtility.DisplayDialog("No Valid Component Found", 
                            "The GameObject doesn't contain any SpriteRenderer, ParticleSystem, or TrailRenderer components.", "OK");
                        return; // 不更新，保持原值
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Object Type", 
                        "Please drag a GameObject or a valid Component (SpriteRenderer, ParticleSystem, or TrailRenderer).", "OK");
                    return; // 不更新，保持原值
                }

                // 验证组件类型
                if (newComponent != null)
                {
                    bool isValid = newComponent is SpriteRenderer || 
                                  newComponent is ParticleSystem || 
                                  newComponent is TrailRenderer;

                    if (!isValid)
                    {
                        EditorUtility.DisplayDialog("Invalid Component Type", 
                            "VFXComponent only supports SpriteRenderer, ParticleSystem, and TrailRenderer.", "OK");
                        return; // 不更新，保持原值
                    }
                }

                // 更新属性值
                componentProp.objectReferenceValue = newComponent;
                property.serializedObject.ApplyModifiedProperties();
                
                // 强制刷新显示
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            // 显示当前组件类型信息
            Rect typeRect = new Rect(position.x + position.width * 0.7f, position.y, position.width * 0.3f, position.height);
            string typeInfo = "None";
            if (currentComponent != null)
            {
                if (currentComponent is SpriteRenderer)
                    typeInfo = "SpriteRenderer";
                else if (currentComponent is ParticleSystem)
                    typeInfo = "ParticleSystem";
                else if (currentComponent is TrailRenderer)
                    typeInfo = "TrailRenderer";
                else
                    typeInfo = "Invalid Type";
            }
            EditorGUI.LabelField(typeRect, typeInfo);

            EditorGUI.EndProperty();
        }

        private Component FindValidComponent(GameObject go)
        {
            // 按优先级查找组件：ParticleSystem > TrailRenderer > SpriteRenderer
            var particleSystem = go.GetComponent<ParticleSystem>();
            if (particleSystem != null)
                return particleSystem;

            var trailRenderer = go.GetComponent<TrailRenderer>();
            if (trailRenderer != null)
                return trailRenderer;

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                return spriteRenderer;

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
} 