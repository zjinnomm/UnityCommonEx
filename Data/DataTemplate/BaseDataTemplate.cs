using Newtonsoft.Json;

namespace UnityCommonEx
{

    public abstract class BaseDataTemplate : BaseData
    {

        [JsonProperty(Required = Required.Default)]
        public string Id = null;

        public virtual void PostInit() { }

        public virtual string Validate() 
        {
            return null;
        }

    }

    public abstract class SingletonDataTemplate<T> : BaseDataTemplate where T : SingletonDataTemplate<T>
    {

        static T _instance;

        public static T Instance => _instance;

        public override void PostInit()
        {
            base.PostInit();
            _instance = (T)this;
        }

    }

}
