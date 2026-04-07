namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置字段的逻辑类型
    /// 目前仅支持 Select，一切字段都抽象为一个“从若干选项中选择其一”的逻辑，
    /// 具体呈现形式（下拉框 / Spinner 等）由对应的 UI Controller 决定。
    /// </summary>
    public enum GameSettingFieldType
    {
        /// <summary>
        /// 选项选择型字段（Select），配合 <c>Options</c> 使用
        /// </summary>
        Select = 0,

        /// <summary>
        /// 数值型字段（Number），配合 Min/Max/Step/Default 使用
        /// </summary>
        Number = 1,
    }

}