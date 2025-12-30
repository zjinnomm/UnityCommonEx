using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public struct UITweenState
    {

        public Vector2 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public float Alpha;

        public static UITweenState Interp(UITweenState start, UITweenState target, UITweenPropType PropType, float t)
        {
            UITweenState result = new UITweenState();
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                result.Position = Vector2.Lerp(start.Position, target.Position, t);
            }
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
            {
                result.Rotation = Quaternion.Lerp(start.Rotation, target.Rotation, t);
            }
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
            {
                result.Scale = Vector3.Lerp(start.Scale, target.Scale, t);
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
            {
                result.Alpha = Mathf.Lerp(start.Alpha, target.Alpha, t);
            }
            return result;
        }

    }

    public class UITween : IPoolable
    {

        public uint Id;

        public UITweenState StartState;
        public UITweenState TargetState;
        
        public float Duration;
        public UITweenPropType PropType;
        public UITweenCurveType CurveType;
        public float Progress;

        public List<UITweenEntry> Tweens = new List<UITweenEntry>();
        public int LoopTimes;
        public int TargetTweenIndex;

        public Transform TargetTransform;
        public Func<float> GetTargetAlphaFunc;
        public Action<float> SetTargetAlphaFunc;
        public Action OnTweenFinished;


        public void OnPoolableReturned()
        {
            Reset();
        }

        public void Reset()
        {
            Tweens.Clear();
            TargetTweenIndex = -1;
            Progress = 0;
            TargetTransform = null;
            GetTargetAlphaFunc = null;
            SetTargetAlphaFunc = null;
            OnTweenFinished = null;
        }

        void ApplyState(UITweenState state)
        {
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                TargetTransform.localPosition = state.Position;
            }
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
            {
                TargetTransform.localRotation = state.Rotation;
            }
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
            {
                TargetTransform.localScale = state.Scale;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0 && SetTargetAlphaFunc != null)
            {
                SetTargetAlphaFunc.Invoke(state.Alpha);
            }
        }

        void PrepareNextTween(int index)
        {
            TargetTweenIndex = index;
            UITweenEntry next = Tweens[index];
            Duration = next.Duration;
            PropType = next.PropType;
            CurveType = next.CurveType;
            Progress = 0;
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                StartState.Position = TargetTransform.localPosition;
                TargetState.Position = next.Position;
                if (next.IsRelative)
                {
                    TargetState.Position += StartState.Position;
                }
            }
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
            {
                StartState.Rotation = TargetTransform.rotation;
                if (next.IsRelative)
                {
                    TargetState.Rotation = Quaternion.Euler(StartState.Rotation.eulerAngles + next.Rotation);
                }
                else
                {
                    TargetState.Rotation = Quaternion.Euler(next.Rotation);
                }
            }
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
            {
                StartState.Scale = TargetTransform.localScale;
                TargetState.Scale = next.Scale;
                if (next.IsRelative)
                {
                    TargetState.Scale += StartState.Scale;
                }
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0 && GetTargetAlphaFunc != null)
            {
                StartState.Alpha = GetTargetAlphaFunc();
                TargetState.Alpha = next.Alpha;
            }
        }

        // return tween finished
        public bool TickTween(float delta)
        {
            if (LoopTimes == 0 || Tweens.Count == 0 || TargetTransform == null)
            {
                return true;
            }
            if (TargetTweenIndex < 0)
            {
                PrepareNextTween(0);
            }
            Progress += delta;
            if (Progress >= Duration)
            {
                ApplyState(TargetState);
                if (TargetTweenIndex == Tweens.Count - 1)
                {
                    LoopTimes--;
                    PrepareNextTween(0);
                }
                else
                {
                    PrepareNextTween(TargetTweenIndex + 1);
                }
            }
            else
            {
                float t = Progress / Duration;
                t = CurveType switch
                {
                    UITweenCurveType.Quad => t * t,
                    UITweenCurveType.QuadReverse => 1 - (1 - t) * (1 - t),
                    _ => t
                };
                ApplyState(UITweenState.Interp(StartState, TargetState, PropType, t));
            }
            return LoopTimes == 0;
        }

        public void ToLastFrame()
        {
            ApplyState(TargetState);
            while (TargetTweenIndex < Tweens.Count - 1)
            {
                PrepareNextTween(TargetTweenIndex + 1);
                ApplyState(TargetState);
            }
        }

    }


}