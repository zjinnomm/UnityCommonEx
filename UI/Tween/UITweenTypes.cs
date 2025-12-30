using UnityEngine;
using System;

namespace UnityCommonEx
{

    public enum UITweenPropType : byte
    {
        Position = 0xb1,
        Rotation = 0b10,
        Scale = 0b100,
        Alpha = 0b1000
    }

    public enum UITweenCurveType : byte
    {
        Linear,
        Quad,
        QuadReverse,
    }

    [Serializable]
    public struct UITweenEntry
    {
        public Vector2 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public float Alpha;
        
        public float Duration;
        public bool IsRelative;
        public UITweenPropType PropType;
        public UITweenCurveType CurveType;
    }

    public struct UITweenObject
    {
        public Transform TargetTransform;
        public Func<float> GetTargetAlphaFunc;
        public Action<float> SetTargetAlphaFunc;
        public Action OnTweenFinished;
    }

}