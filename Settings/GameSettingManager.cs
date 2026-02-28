using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置管理器接口（非泛型版本，用于UI控制器）
    /// </summary>
    public interface IGameSettingManager
    {
        object GetValue(string fieldName);
        void SetValue(string fieldName, object value);
    }

    /// <summary>
    /// 游戏设置管理器，负责设置的加载、保存和回调管理
    /// </summary>
    public class GameSettingManager<T> : Singleton<GameSettingManager<T>>, IGameSettingManager where T : GameSetting, new()
    {

        private T _settings;
        private string _savePath;
        private Dictionary<string, Action<object>> _callbacks = new Dictionary<string, Action<object>>();
        private Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// 当前设置实例
        /// </summary>
        public T Settings => _settings;

        /// <summary>
        /// 初始化管理器，指定保存路径
        /// </summary>
        /// <param name="savePath">设置保存路径（相对于项目根目录或绝对路径）</param>
        public void Initialize(string savePath)
        {
            _savePath = savePath;
            _settings = new T();
            
            // 缓存字段信息
            CacheFields();
            
            // 自动加载设置
            Load();
        }

        /// <summary>
        /// 根据模板应用字段的 OnChangedFunc：注册变更回调，并在加载完成后对每个配置了 OnChangedFunc 的字段调用一次。
        /// 应在 Initialize 之后、由持有 template 的调用方执行（如游戏入口或设置 UI 打开时）。
        /// </summary>
        public void ApplyFieldCallbacks(GameSettingTemplate template)
        {
            if (template?.Fields == null)
                return;

            foreach (var fieldConfig in template.Fields)
            {
                if (fieldConfig?.OnChangedFunc == null || string.IsNullOrEmpty(fieldConfig.OnChangedFunc.Function))
                    continue;

                string fieldName = fieldConfig.FieldName;
                if (!_fieldCache.ContainsKey(fieldName))
                    continue;

                fieldConfig.OnChangedFunc.Init(new[] { typeof(object) }, typeof(void), $"{typeof(T).Name}.{fieldName}");

                RegisterCallback(fieldName, value =>
                {
                    fieldConfig.OnChangedFunc.TryInvoke(new[] { value }, out _);
                });

                object currentValue = GetValue(fieldName);
                if (currentValue != null)
                    fieldConfig.OnChangedFunc.TryInvoke(new[] { currentValue }, out _);
            }
        }

        /// <summary>
        /// 缓存所有字段信息
        /// </summary>
        private void CacheFields()
        {
            _fieldCache.Clear();
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                _fieldCache[field.Name] = field;
            }
        }

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        public void Load()
        {
            if (string.IsNullOrEmpty(_savePath))
            {
                LogUtil.Warn("GameSettingManager: SavePath is not set, using default settings");
                return;
            }

            try
            {
                // 构建完整路径
                string fullPath = GetFullPath(_savePath);
                
                if (System.IO.File.Exists(fullPath))
                {
                    _settings = JsonUtil.Read<T>(fullPath);
                    if (_settings == null)
                    {
                        LogUtil.Warn("GameSettingManager: Failed to load settings, using default");
                        _settings = new T();
                    }
                }
                else
                {
                    LogUtil.Info("GameSettingManager: Settings file not found, using default settings");
                    _settings = new T();
                    // 保存默认设置
                    Save();
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error loading settings: {0}", ex.Message);
                _settings = new T();
            }
        }

        /// <summary>
        /// 保存设置到文件
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(_savePath))
            {
                LogUtil.Warn("GameSettingManager: SavePath is not set, cannot save");
                return;
            }

            try
            {
                string fullPath = GetFullPath(_savePath);
                JsonUtil.Write(fullPath, _settings);
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error saving settings: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 获取完整路径（支持相对路径和绝对路径）
        /// </summary>
        private string GetFullPath(string path)
        {
            return path;
        }

        /// <summary>
        /// 注册字段修改回调
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="callback">回调函数</param>
        public void RegisterCallback(string fieldName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                LogUtil.Warn("GameSettingManager: FieldName cannot be null or empty");
                return;
            }

            if (!_fieldCache.ContainsKey(fieldName))
            {
                LogUtil.Warn("GameSettingManager: Field '{0}' not found in {1}", fieldName, typeof(T).Name);
                return;
            }

            if (callback != null)
            {
                _callbacks[fieldName] = callback;
            }
            else if (_callbacks.ContainsKey(fieldName))
            {
                _callbacks.Remove(fieldName);
            }
        }

        /// <summary>
        /// 设置字段值（会触发保存和回调）
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="value">新值</param>
        public void SetValue(string fieldName, object value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                LogUtil.Warn("GameSettingManager: FieldName cannot be null or empty");
                return;
            }

            if (!_fieldCache.TryGetValue(fieldName, out FieldInfo field))
            {
                LogUtil.Warn("GameSettingManager: Field '{0}' not found in {1}", fieldName, typeof(T).Name);
                return;
            }

            try
            {
                // 类型转换
                object convertedValue = ConvertValue(value, field.FieldType);
                
                // 设置值
                field.SetValue(_settings, convertedValue);
                
                // 触发回调
                if (_callbacks.TryGetValue(fieldName, out Action<object> callback))
                {
                    callback?.Invoke(convertedValue);
                }
                
                // 自动保存
                Save();
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error setting field '{0}': {1}", fieldName, ex.Message);
            }
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns>字段值</returns>
        public object GetValue(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                LogUtil.Warn("GameSettingManager: FieldName cannot be null or empty");
                return null;
            }

            if (!_fieldCache.TryGetValue(fieldName, out FieldInfo field))
            {
                LogUtil.Warn("GameSettingManager: Field '{0}' not found in {1}", fieldName, typeof(T).Name);
                return null;
            }

            try
            {
                return field.GetValue(_settings);
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error getting field '{0}': {1}", fieldName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 类型转换辅助方法
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            Type valueType = value.GetType();
            
            // 如果类型匹配，直接返回
            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            // 处理可空类型
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }

            // 尝试转换
            if (targetType.IsEnum)
            {
                if (value is string strValue)
                {
                    return Enum.Parse(targetType, strValue);
                }
                return Enum.ToObject(targetType, value);
            }

            // 基本类型转换
            return Convert.ChangeType(value, targetType);
        }

    }

}