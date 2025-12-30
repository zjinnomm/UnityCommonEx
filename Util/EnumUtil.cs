using System;

namespace UnityCommonEx
{
    public static class EnumUtil
    {

        public static int Max(Type enumType)
        {
            int max = int.MinValue;
            Array enumArray = Enum.GetValues(enumType);
            if (enumArray != null)
            {
                foreach (var e in enumArray)
                {
                    int ee = Convert.ToInt32(e);
                    if (ee > max)
                    {
                        max = ee;
                    }
                }
            }
            return max;
        }

        public static int Min(Type enumType)
        {
            int min = int.MaxValue;
            Array enumArray = Enum.GetValues(enumType);
            if (enumArray != null)
            {
                foreach (var e in enumArray)
                {
                    int ee = Convert.ToInt32(e);
                    if (ee < min)
                    {
                        min = ee;
                    }
                }
            }
            return min;
        }

    }
}