using UnityEngine;

namespace UnityCommonEx
{

    public static class TimeUtil
    {

        public static string FormatMS(float time)
        {
            int timeInt = Mathf.FloorToInt(time);
            return $"{timeInt / 60:D2}:{timeInt % 60:D2}";
        }

        public static string FormatMSM(float time)
        {
            int timeInt = Mathf.FloorToInt(time);
            return $"{timeInt / 60:D2}:{timeInt % 60:D2}.{Mathf.FloorToInt((time - timeInt) * 1000):D3}";
        }

        public static string FormatSM(float time)
        {
            int timeInt = Mathf.FloorToInt(time);
            return $"{timeInt}.{Mathf.FloorToInt((time - timeInt) * 1000):D3}";
        }

    }

}