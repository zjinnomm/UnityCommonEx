namespace UnityCommonEx
{

    public class SingletonController<T> : NodeController where T : SingletonController<T>
    {

        private static T _instance;
        public static T Instance => _instance;

        protected virtual bool IsPersistent => false;

        protected override void OnInit()
        {
            base.OnInit();
            _instance = (T)this;
        }

        protected override void OnRelease()
        {
            if (!IsPersistent && _instance == this)
            {
                _instance = null;
            }
            base.OnRelease();
        }

    }

}