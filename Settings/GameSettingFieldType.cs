namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置字段的逻辑类型
    /// 目前支持 Select / Number / Toggle，具体呈现形式由对应的 UI Controller 决定。
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

        /// <summary>
        /// 开关型字段（Toggle），配合 Default 使用
        /// </summary>
        Toggle = 2,
    }

}
