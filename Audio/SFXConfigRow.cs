using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// 音效配置行：用于定义音效 Key 与 AudioClip 资源路径
    /// </summary>
    public class SFXConfigRow : BaseDataTableRow<string>
    {
        public string Key;
        public string Path; // AudioClip 的资源路径（相对于 SFXRootResPath）

        public override string RowKey => Key;
    }
}

