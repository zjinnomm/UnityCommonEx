namespace UnityCommonEx
{

    public abstract class BaseDataTableRow : BaseData
    {

        public virtual void PostInit() { }

        public virtual string Validate() 
        {
            return null;
        }

    }

    public abstract class BaseDataTableRow<T> : BaseDataTableRow
    {

        public abstract T RowKey { get; } 

    }

}