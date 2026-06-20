using System;
using UnityEngine;

namespace UnityCommonEx
{

    public struct UITweenState
    {
        public Vector2 Position;
        public float Alpha;
        public float SizeX;
        public float SizeY;
        public float Scale;
        public Vector3 Rotation;
    }

    public class UITween : IPoolable
    {
        public uint Id;

        public UITweenState StartState;
        public UITweenState TargetState;

        public UITweenPropType PropType;
        public float Progress;

        public UITweenConfig Config;
        public int LoopTimes;
        public bool IsActive;

        public RectTransform TargetTransform;
        public Func<float> GetTargetAlphaFunc;
        public Action<float> SetTargetAlphaFunc;
        public Action OnTweenFinished;

        public void OnPoolableReturned()
        {
            Reset();
        }

        public void Reset()
        {
            IsActive = false;
            Progress = 0;
            TargetTransform = null;
            GetTargetAlphaFunc = null;
            SetTargetAlphaFunc = null;
            OnTweenFinished = null;
        }

        void ApplyState(UITweenState state)
        {
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0 && TargetTransform != null)
                TargetTransform.anchoredPosition = state.Position;

            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0 && SetTargetAlphaFunc != null)
                SetTargetAlphaFunc.Invoke(state.Alpha);

            if (TargetTransform == null)
                return;

            Vector2 sizeDelta = TargetTransform.sizeDelta;
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
                sizeDelta.x = state.SizeX;
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
                sizeDelta.y = state.SizeY;
            TargetTransform.sizeDelta = sizeDelta;

            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
            {
                float scale = SanitizeScale(state.Scale, TargetTransform.localScale.x);
                TargetTransform.localScale = new Vector3(scale, scale, scale);
            }

            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
                TargetTransform.localEulerAngles = state.Rotation;
        }

        public void PrepareTween()
        {
            Progress = 0;

            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                StartState.Position = Config.MinPos;
                TargetState.Position = Config.MaxPos;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
            {
                StartState.Alpha = Config.MinAlpha;
                TargetState.Alpha = Config.MaxAlpha;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
            {
                StartState.SizeX = Config.MinSizeX;
                TargetState.SizeX = Config.MaxSizeX;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
            {
                StartState.SizeY = Config.MinSizeY;
                TargetState.SizeY = Config.MaxSizeY;
            }
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
            {
                StartState.Scale = Config.MinScale;
                TargetState.Scale = Config.MaxScale;
            }
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
            {
                StartState.Rotation = Config.MinRot;
                TargetState.Rotation = Config.MaxRot;
            }

            ApplyInitialState();
        }

        void ApplyInitialState()
        {
            UITweenState initialState = GetCurrentState();

            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
                initialState.Position = EvaluatePosition(0f, Config.MinPos, Config.MaxPos);
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
                initialState.Alpha = EvaluateScalar(0f, Config.MinAlpha, Config.MaxAlpha, Config.AlphaCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
                initialState.SizeX = EvaluateScalar(0f, Config.MinSizeX, Config.MaxSizeX, Config.SizeXCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
                initialState.SizeY = EvaluateScalar(0f, Config.MinSizeY, Config.MaxSizeY, Config.SizeYCurve);
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
                initialState.Scale = EvaluateScalar(0f, Config.MinScale, Config.MaxScale, Config.ScaleCurve);
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
                initialState.Rotation = EvaluateRotation(0f, Config.MinRot, Config.MaxRot);

            ApplyState(initialState);
        }

        public bool TickTween(float delta)
        {
            if (LoopTimes == 0 || TargetTransform == null || !IsActive)
                return true;

            if (Progress == 0)
                PrepareTween();

            Progress += delta;

            float duration = GetDuration();
            if (duration <= 0)
                duration = 1f;

            if (Progress >= duration)
            {
                ApplyFinalState();
                LoopTimes--;
                if (LoopTimes > 0)
                {
                    Progress = 0;
                    PrepareTween();
                }
                else
                {
                    return true;
                }
            }
            else
            {
                ApplyStateWithCurves(Progress);
            }
            return LoopTimes == 0;
        }

        void ApplyFinalState()
        {
            UITweenState finalState = GetCurrentState();

            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
                finalState.Position = EvaluatePosition(1f, Config.MinPos, Config.MaxPos);
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
                finalState.Alpha = EvaluateScalar(1f, Config.MinAlpha, Config.MaxAlpha, Config.AlphaCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
                finalState.SizeX = EvaluateScalar(1f, Config.MinSizeX, Config.MaxSizeX, Config.SizeXCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
                finalState.SizeY = EvaluateScalar(1f, Config.MinSizeY, Config.MaxSizeY, Config.SizeYCurve);
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
                finalState.Scale = EvaluateScalar(1f, Config.MinScale, Config.MaxScale, Config.ScaleCurve);
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
                finalState.Rotation = EvaluateRotation(1f, Config.MinRot, Config.MaxRot);

            ApplyState(finalState);
        }

        public void ApplyStateWithCurves(float currentProgress)
        {
            UITweenState state = new UITweenState();
            float duration = GetDuration();
            float normalizedT = duration > 0 ? Mathf.Clamp01(currentProgress / duration) : 1f;

            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
                state.Position = EvaluatePosition(normalizedT, StartState.Position, TargetState.Position);
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
                state.Alpha = EvaluateScalar(normalizedT, StartState.Alpha, TargetState.Alpha, Config.AlphaCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
                state.SizeX = EvaluateScalar(normalizedT, StartState.SizeX, TargetState.SizeX, Config.SizeXCurve);
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
                state.SizeY = EvaluateScalar(normalizedT, StartState.SizeY, TargetState.SizeY, Config.SizeYCurve);
            if (((byte)PropType & (byte)UITweenPropType.Scale) > 0)
                state.Scale = EvaluateScalar(normalizedT, StartState.Scale, TargetState.Scale, Config.ScaleCurve);
            if (((byte)PropType & (byte)UITweenPropType.Rotation) > 0)
                state.Rotation = EvaluateRotation(normalizedT, StartState.Rotation, TargetState.Rotation);

            ApplyState(state);
        }

        public float GetDuration()
        {
            if (Config.Duration > 0)
                return Config.Duration;
            return 1f;
        }

        public float GetProgress()
        {
            float duration = GetDuration();
            if (duration > 0)
                return Mathf.Clamp01(Progress / duration);
            return 1f;
        }

        public void ToLastFrame()
        {
            ApplyFinalState();
        }

        private UITweenState GetCurrentState()
        {
            UITweenState state = new UITweenState();
            if (TargetTransform != null)
            {
                state.Position = TargetTransform.anchoredPosition;
                state.SizeX = TargetTransform.sizeDelta.x;
                state.SizeY = TargetTransform.sizeDelta.y;
                state.Scale = TargetTransform.localScale.x;
                state.Rotation = TargetTransform.localEulerAngles;
            }
            if (GetTargetAlphaFunc != null)
                state.Alpha = GetTargetAlphaFunc();
            return state;
        }

        private Vector2 EvaluatePosition(float normalizedT, Vector2 start, Vector2 target)
        {
            Vector2 pos = start;
            pos.x = EvaluateScalar(normalizedT, start.x, target.x, Config.PosXCurve);
            pos.y = EvaluateScalar(normalizedT, start.y, target.y, Config.PosYCurve);
            return pos;
        }

        private Vector3 EvaluateRotation(float normalizedT, Vector3 start, Vector3 target)
        {
            Vector3 rotation = start;
            rotation.x = EvaluateScalar(normalizedT, start.x, target.x, Config.RotXCurve);
            rotation.y = EvaluateScalar(normalizedT, start.y, target.y, Config.RotYCurve);
            rotation.z = EvaluateScalar(normalizedT, start.z, target.z, Config.RotZCurve);
            return rotation;
        }

        private static float EvaluateScalar(float normalizedT, float start, float target, AnimationCurve curve)
        {
            if (curve != null && curve.length > 0)
            {
                float curveValue = curve.Evaluate(normalizedT);
                return Mathf.Lerp(start, target, curveValue);
            }
            return Mathf.Lerp(start, target, normalizedT);
        }

        private static float SanitizeScale(float candidate, float fallback)
        {
            if (IsFinite(candidate))
                return candidate;
            if (IsFinite(fallback))
                return fallback;
            return 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

}
