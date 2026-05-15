using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    public class GameSettingTemplate : BaseDataTemplate
    {
        [JsonProperty(Required = Required.Default)]
        public GameSettingCategoryConfig[] Categories;

        public IEnumerable<GameSettingCategoryConfig> EnumerateCategories()
        {
            if (Categories == null)
                yield break;

            for (int i = 0; i < Categories.Length; i++)
            {
                GameSettingCategoryConfig category = Categories[i];
                if (category == null)
                    continue;

                yield return category;
            }
        }

        public IEnumerable<GameSettingGroupConfig> EnumerateGroups()
        {
            foreach (GameSettingCategoryConfig category in EnumerateCategories())
            {
                if (category.Groups == null)
                    continue;

                for (int i = 0; i < category.Groups.Length; i++)
                {
                    GameSettingGroupConfig group = category.Groups[i];
                    if (group == null)
                        continue;

                    yield return group;
                }
            }
        }

        public IEnumerable<GameSettingFieldConfig> EnumerateFields()
        {
            foreach (GameSettingGroupConfig group in EnumerateGroups())
            {
                if (group.Fields == null)
                    continue;

                for (int i = 0; i < group.Fields.Length; i++)
                {
                    GameSettingFieldConfig field = group.Fields[i];
                    if (field == null)
                        continue;

                    yield return field;
                }
            }
        }

        public override string Validate()
        {
            foreach (GameSettingFieldConfig field in EnumerateFields())
            {
                if (field != null)
                    return null;
            }

            return "Categories is empty";
        }
    }

    public class GameSettingCategoryConfig
    {
        [JsonProperty(Required = Required.Default)]
        public string CategoryName;

        [JsonProperty(Required = Required.Default)]
        public MultiLingualText DisplayName;

        [JsonProperty(Required = Required.Default)]
        public GameSettingGroupConfig[] Groups;
    }

    public class GameSettingGroupConfig
    {
        [JsonProperty(Required = Required.Default)]
        public string GroupName;

        [JsonProperty(Required = Required.Default)]
        public MultiLingualText DisplayName;

        [JsonProperty(Required = Required.Default)]
        public GameSettingFieldConfig[] Fields;
    }
}
