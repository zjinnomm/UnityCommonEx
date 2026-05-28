using UnityEngine;
using System;

namespace UnityCommonEx
{

    public enum UITweenPropType : byte
    {
        Position = 0b1,
        Alpha = 0b10,
        SizeX = 0b100,
        SizeY = 0b1000,
        Scale = 0b10000,
        Rotation = 0b100000
    }

    public static class UITweenPropTypeUtil
    {
        public const byte ValidMask =
            (byte)UITweenPropType.Position |
            (byte)UITweenPropType.Alpha |
            (byte)UITweenPropType.SizeX |
            (byte)UITweenPropType.SizeY |
            (byte)UITweenPropType.Scale |
            (byte)UITweenPropType.Rotation;

        public static UITweenPropType Sanitize(UITweenPropType propType)
        {
            return (UITweenPropType)((byte)propType & ValidMask);
        }

        public static bool HasInvalidBits(UITweenPropType propType)
        {
            return ((byte)propType & ~ValidMask) != 0;
        }
    }

    [Serializable]
    public struct UITweenConfig
    {
        public UITweenPropType PropType;
        public float Duration;

        public Vector2 MinPos;
        public Vector2 MaxPos;
        public AnimationCurve PosXCurve;
        public AnimationCurve PosYCurve;

        public float MinAlpha;
        public float MaxAlpha;
        public AnimationCurve AlphaCurve;

        public float MinSizeX;
        public float MaxSizeX;
        public AnimationCurve SizeXCurve;

        public float MinSizeY;
        public float MaxSizeY;
        public AnimationCurve SizeYCurve;

        public float MinScale;
        public float MaxScale;
        public AnimationCurve ScaleCurve;

        public Vector3 MinRot;
        public Vector3 MaxRot;
        public AnimationCurve RotXCurve;
        public AnimationCurve RotYCurve;
        public AnimationCurve RotZCurve;
    }

    public struct UITweenObject
    {
        public RectTransform TargetTransform;
        public Func<float> GetTargetAlphaFunc;
        public Action<float> SetTargetAlphaFunc;
        public Action OnTweenFinished;
    }

}
