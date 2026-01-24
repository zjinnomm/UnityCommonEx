using Newtonsoft.Json;

namespace UnityCommonEx
{

    /// <summary>
    /// 游戏设置模板，定义设置的UI显示和编辑规则
    /// </summary>
    public class GameSettingTemplate : BaseDataTemplate
    {

        /// <summary>
        /// 所有字段的配置信息
        /// </summary>
        public GameSettingFieldConfig[] Fields;

    }

}