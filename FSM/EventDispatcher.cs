using System;
using System.Collections.Generic;

namespace UnityCommonEx
{

    public delegate void EventResolver(Properties properties);

    public class EventDispatcher<T> where T : Enum
    {

        private Dictionary<T, EventResolver> _delegates;

        public EventDispatcher()
        {
            _delegates = new Dictionary<T, EventResolver>();
        }

        public void Register(T eventType, EventResolver resolver)
        {
            if (_delegates.TryGetValue(eventType, out var registered))
            {
                _delegates[eventType] = registered + resolver;
            }
            else
            {
                _delegates[eventType] = resolver;
            }
        }

        public void UnRegister(T eventType, EventResolver resolver)
        {
            if (_delegates.TryGetValue(eventType, out var registered))
            {
                registered -= resolver;
                if (registered == null)
                {
                    _delegates.Remove(eventType);
                }
                else
                {
                    _delegates[eventType] = registered;
                }
            }
        }

        public void Trigger(T eventType, Properties properties = null)
        {
            if (_delegates.TryGetValue(eventType, out var resolver))
            {
                resolver.Invoke(properties);
            }
        }

    }

}