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
        Scale = 0b10000
    }

    [Serializable]
    public struct UITweenConfig
    {
        public UITweenPropType PropType;
        
        /// <summary>
        /// 动画持续时间（秒）。所有曲线的时间范围应该是 0-1（相对于 Duration 归一化）
        /// </summary>
        public float Duration;
        
        // Position properties
        public Vector2 MinPos;      // 曲线计算的起始值（t=0）
        public Vector2 MaxPos;      // 曲线计算的结束值（t=1）
        public AnimationCurve PosXCurve;  // 时间范围 0-1（相对于 Duration）
        public AnimationCurve PosYCurve;  // 时间范围 0-1（相对于 Duration）
        
        // Alpha properties
        public float MinAlpha;      // 曲线计算的起始值（t=0）
        public float MaxAlpha;      // 曲线计算的结束值（t=1）
        public AnimationCurve AlphaCurve;  // 时间范围 0-1（相对于 Duration）
        
        // SizeX properties
        public float MinSizeX;      // 曲线计算的起始值（t=0）
        public float MaxSizeX;      // 曲线计算的结束值（t=1）
        public AnimationCurve SizeXCurve;  // 时间范围 0-1（相对于 Duration）
        
        // SizeY properties
        public float MinSizeY;      // 曲线计算的起始值（t=0）
        public float MaxSizeY;      // 曲线计算的结束值（t=1）
        public AnimationCurve SizeYCurve;  // 时间范围 0-1（相对于 Duration）

        // Scale properties
        public float MinScale;      // 曲线计算的起始值（t=0），统一作用于 XYZ
        public float MaxScale;      // 曲线计算的结束值（t=1），统一作用于 XYZ
        public AnimationCurve ScaleCurve;  // 时间范围 0-1（相对于 Duration），统一作用于 XYZ
    }

    public struct UITweenObject
    {
        public RectTransform TargetTransform;
        public Func<float> GetTargetAlphaFunc;
        public Action<float> SetTargetAlphaFunc;
        public Action OnTweenFinished;
    }

}
