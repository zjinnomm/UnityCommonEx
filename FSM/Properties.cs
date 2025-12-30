using System.Collections.Generic;

namespace UnityCommonEx
{

    public class Properties
    {

        private Dictionary<string, object> _properties;

        public Properties(params object[] parameters)
        {
            _properties = new Dictionary<string, object>();
            for (int i = 0; i < parameters.Length - 1; i += 2)
            {
                _properties.Add(parameters[i].ToString(), parameters[i + 1]);
            }
        }

        public void Put<T>(string key, T value)
        {
            _properties.Add(key, value);
        }

        public bool Contains(string key)
        {
            return _properties.ContainsKey(key);
        }

        public T Get<T>(string key)
        {
            return (T)_properties[key];
        }

        public T GetOrDefault<T>(string key, T def)
        {
            T value = def;
            if (_properties.ContainsKey(key))
            {
                var v = _properties[key];
                if (v is T)
                {
                    value = (T)v;
                }
            }
            return value;
        }

    }

}