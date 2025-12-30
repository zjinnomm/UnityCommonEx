using System;

namespace UnityCommonEx
{

    public class AttributeConfig<T> : BaseDataTableRow<T> where T : Enum
    {

        public T Name;
        public float Default;
        public float Min = float.MinValue;
        public float Max = float.MaxValue;

        public override T RowKey => Name;

    }

    public partial class AttributeSet<T> where T : Enum
    {

        public static void InitAttributeConfig<TR>(DataTable<T, TR> table) where TR : AttributeConfig<T>, new()
        {
            foreach (var config in table)
            {
                int index = Convert.ToInt32(config.Value.Name);
                defaultValues[index] = config.Value.Default;
                minValues[index] = config.Value.Min;
                maxValues[index] = config.Value.Max;
            }
        }
}

}