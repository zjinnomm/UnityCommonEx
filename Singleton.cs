namespace UnityCommonEx
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {

        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }

    }

    public abstract class SemiSingleton<T> where T : SemiSingleton<T>
    {

        private static T _instance;

        public static T Instance => _instance;

        public static void InitInstance(T instance)
        {
            _instance = instance;
        }

        public static void ClearInstance()
        {
            _instance = null;
        }

    }

}