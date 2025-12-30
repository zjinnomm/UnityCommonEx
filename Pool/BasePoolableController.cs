namespace UnityCommonEx
{
    public abstract class BasePoolableController : NodeController, IPoolable
    {
        public virtual bool IsPoolableReusable()
        {
            return this;
        }

        public virtual void OnPoolableDestroyed()
        {
            if (this)
            {
                Destroy();
            }
            
        }

        public virtual void OnPoolableReturned()
        {
            Deactivate();
        }

        public virtual void OnPoolableTaken()
        {
            Activate();
        }
    }

    public abstract class BaseCategorizedPoolableController : BasePoolableController, ICategorizedPoolable<string>
    {
        public string Name { get; protected set; }

        public string GetPoolCategory() => Name;

        public void SetPoolCategory(string category)
        {
            Name = category;
        }
    }

}