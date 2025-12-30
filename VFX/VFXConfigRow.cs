namespace UnityCommonEx
{

    public class VFXConfigRow : BaseDataTableRow<string>
    {

        public string Key;
        public string Path;

        public override string RowKey => Key;
        
    }

}