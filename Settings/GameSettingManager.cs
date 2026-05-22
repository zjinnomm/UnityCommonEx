using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityCommonEx
{

    public interface IGameSettingManager
    {
        object GetValue(string fieldName);
        TValue GetValue<TValue>(string fieldName, TValue fallback = default);
        void SetValue(string fieldName, object value);
    }

    public class GameSettingManager<T> : Singleton<GameSettingManager<T>>, IGameSettingManager where T : GameSetting, new()
    {
        private T _settings;
        private T _committedSettings;
        private string _savePath;
        private readonly Dictionary<string, Action<object>> _callbacks = new Dictionary<string, Action<object>>();
        private readonly HashSet<string> _immediateFields = new HashSet<string>();
        private readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();

        public T Settings => _settings;
        public bool HasPendingChanges => !AreSettingsEqual(_settings, _committedSettings);

        public void Initialize(string savePath)
        {
            _savePath = savePath;
            _settings = new T();
            _committedSettings = new T();

            CacheFields();
            Load();
        }

        public void ApplyFieldCallbacks(GameSettingTemplate template)
        {
            if (template == null)
                return;

            _immediateFields.Clear();

            foreach (var fieldConfig in template.EnumerateFields())
            {
                if (fieldConfig == null)
                    continue;

                if (fieldConfig.ChangeImmediately)
                    _immediateFields.Add(fieldConfig.FieldName);

                if (fieldConfig.OnChangedFunc == null || string.IsNullOrEmpty(fieldConfig.OnChangedFunc.Function))
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

        public void Load()
        {
            if (string.IsNullOrEmpty(_savePath))
            {
                LogUtil.Warn("GameSettingManager: SavePath is not set, using default settings");
                _settings = new T();
                _committedSettings = CloneSettings(_settings);
                return;
            }

            try
            {
                string fullPath = GetFullPath(_savePath);

                if (System.IO.File.Exists(fullPath))
                {
                    _settings = JsonUtil.Read<T>(fullPath, JsonReadFailureMode.Warning);
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
                    Save();
                }

                _committedSettings = CloneSettings(_settings);
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error loading settings: {0}", ex.Message);
                _settings = new T();
                _committedSettings = CloneSettings(_settings);
            }
        }

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
                _committedSettings = CloneSettings(_settings);
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error saving settings: {0}", ex.Message);
            }
        }

        public void ApplyChanges()
        {
            try
            {
                foreach (var kv in _fieldCache)
                {
                    object currentValue = kv.Value.GetValue(_settings);
                    object committedValue = _committedSettings != null ? kv.Value.GetValue(_committedSettings) : null;
                    if (ValuesEqual(currentValue, committedValue))
                        continue;

                    if (_immediateFields.Contains(kv.Key))
                        continue;

                    if (_callbacks.TryGetValue(kv.Key, out Action<object> callback))
                        callback?.Invoke(currentValue);
                }

                Save();
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error applying settings: {0}", ex.Message);
            }
        }

        public void RevertChanges()
        {
            foreach (var fieldName in _immediateFields)
            {
                if (!_fieldCache.TryGetValue(fieldName, out FieldInfo field))
                    continue;

                object currentValue = field.GetValue(_settings);
                object committedValue = _committedSettings != null ? field.GetValue(_committedSettings) : null;
                if (ValuesEqual(currentValue, committedValue))
                    continue;

                if (_callbacks.TryGetValue(fieldName, out Action<object> callback))
                    callback?.Invoke(committedValue);
            }

            _settings = CloneSettings(_committedSettings ?? new T());
        }

        private string GetFullPath(string path)
        {
            return path;
        }

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
                object currentValue = field.GetValue(_settings);
                object convertedValue = ConvertValue(value, field.FieldType);
                if (ValuesEqual(currentValue, convertedValue))
                    return;

                field.SetValue(_settings, convertedValue);

                if (_immediateFields.Contains(fieldName) && _callbacks.TryGetValue(fieldName, out Action<object> callback))
                    callback?.Invoke(convertedValue);
            }
            catch (Exception ex)
            {
                LogUtil.Error("GameSettingManager: Error setting field '{0}': {1}", fieldName, ex.Message);
            }
        }

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

        public TValue GetValue<TValue>(string fieldName, TValue fallback = default)
        {
            object value = GetValue(fieldName);
            if (value == null)
                return fallback;

            try
            {
                object converted = ConvertValue(value, typeof(TValue));
                if (converted is TValue typedValue)
                    return typedValue;
            }
            catch (Exception ex)
            {
                LogUtil.Warn("GameSettingManager: Error converting field '{0}' to {1}: {2}", fieldName, typeof(TValue).Name, ex.Message);
            }

            return fallback;
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            Type valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
                return value;

            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                targetType = underlyingType;

            if (targetType == typeof(string))
                return value.ToString();

            if (targetType.IsEnum)
            {
                if (value is string strValue)
                    return Enum.Parse(targetType, strValue);
                return Enum.ToObject(targetType, value);
            }

            return Convert.ChangeType(value, targetType);
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;
            return Equals(left, right);
        }

        private static bool AreSettingsEqual(T left, T right)
        {
            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;
            return JsonUtil.WriteRaw(left) == JsonUtil.WriteRaw(right);
        }

        private static T CloneSettings(T source)
        {
            if (source == null)
                return new T();
            return JsonUtil.ReadRaw<T>(JsonUtil.WriteRaw(source));
        }
    }
}
