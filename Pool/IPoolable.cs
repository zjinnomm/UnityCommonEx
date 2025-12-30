namespace UnityCommonEx
{

    public interface IPoolable
    {

        public void OnPoolableCreated() { }
        public void OnPoolableTaken() { }
        public void OnPoolableReturned() { }
        public void OnPoolableDestroyed() { }
        public bool IsPoolableReusable() { return true; }

    }

    public interface ICategorizedPoolable<T> : IPoolable
    {

        public T GetPoolCategory();

        public void SetPoolCategory(T category);

    }

}