namespace UnityCommonEx
{
    /// <summary>
    /// 背景音乐配置行：用于定义 BGM Key 与 AudioClip 资源路径
    /// </summary>
    public class BGMConfigRow : BaseDataTableRow<string>
    {
        public string Key;
        public string Path; // AudioClip 的资源路径（相对于 BGMRootResPath）

        public override string RowKey => Key;
    }
}
