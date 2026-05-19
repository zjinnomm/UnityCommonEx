using System;

namespace UnityCommonEx
{
    public static class LanguageTypeEx
    {
        public static Func<LanguageType> GetLanguageType { get; set; }

        public static LanguageType GetCurrentLanguageType()
        {
            return GetLanguageType != null ? GetLanguageType() : LanguageType.English;
        }
    }
}
