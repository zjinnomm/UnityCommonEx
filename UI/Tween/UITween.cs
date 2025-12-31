using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public struct UITweenState
    {
        public Vector2 Position;
        public float Alpha;
        public float SizeX;
        public float SizeY;
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
            {
                TargetTransform.anchoredPosition = state.Position;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0 && SetTargetAlphaFunc != null)
            {
                SetTargetAlphaFunc.Invoke(state.Alpha);
            }
            if (TargetTransform != null)
            {
                Vector2 sizeDelta = TargetTransform.sizeDelta;
                if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
                {
                    sizeDelta.x = state.SizeX;
                }
                if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
                {
                    sizeDelta.y = state.SizeY;
                }
                TargetTransform.sizeDelta = sizeDelta;
            }
        }

        public void PrepareTween()
        {
            Progress = 0;
            
            // 设置起始值和目标值（从 Min 到 Max）
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
            
            // 第一次 Tick 前，通过曲线初端（t=0）设置初始状态
            ApplyInitialState();
        }
        
        void ApplyInitialState()
        {
            UITweenState initialState = new UITweenState();
            
            // 获取当前位置、Alpha 和 Size（用于不参与动画的属性）
            if (TargetTransform != null)
            {
                initialState.Position = TargetTransform.anchoredPosition;
                initialState.SizeX = TargetTransform.sizeDelta.x;
                initialState.SizeY = TargetTransform.sizeDelta.y;
            }
            if (GetTargetAlphaFunc != null)
            {
                initialState.Alpha = GetTargetAlphaFunc();
            }
            
            // 根据 PropType 和曲线初端（t=0）设置初始值
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                Vector2 pos = Config.MinPos;
                if (Config.PosXCurve != null && Config.PosXCurve.length > 0)
                {
                    float curveValue = Config.PosXCurve.Evaluate(0f);
                    pos.x = Mathf.Lerp(Config.MinPos.x, Config.MaxPos.x, curveValue);
                }
                if (Config.PosYCurve != null && Config.PosYCurve.length > 0)
                {
                    float curveValue = Config.PosYCurve.Evaluate(0f);
                    pos.y = Mathf.Lerp(Config.MinPos.y, Config.MaxPos.y, curveValue);
                }
                initialState.Position = pos;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
            {
                float alpha = Config.MinAlpha;
                if (Config.AlphaCurve != null && Config.AlphaCurve.length > 0)
                {
                    float curveValue = Config.AlphaCurve.Evaluate(0f);
                    alpha = Mathf.Lerp(Config.MinAlpha, Config.MaxAlpha, curveValue);
                }
                initialState.Alpha = alpha;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
            {
                float sizeX = Config.MinSizeX;
                if (Config.SizeXCurve != null && Config.SizeXCurve.length > 0)
                {
                    float curveValue = Config.SizeXCurve.Evaluate(0f);
                    sizeX = Mathf.Lerp(Config.MinSizeX, Config.MaxSizeX, curveValue);
                }
                initialState.SizeX = sizeX;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
            {
                float sizeY = Config.MinSizeY;
                if (Config.SizeYCurve != null && Config.SizeYCurve.length > 0)
                {
                    float curveValue = Config.SizeYCurve.Evaluate(0f);
                    sizeY = Mathf.Lerp(Config.MinSizeY, Config.MaxSizeY, curveValue);
                }
                initialState.SizeY = sizeY;
            }
            
            ApplyState(initialState);
        }

        // return tween finished
        public bool TickTween(float delta)
        {
            if (LoopTimes == 0 || TargetTransform == null || !IsActive)
            {
                return true;
            }
            
            if (Progress == 0)
            {
                PrepareTween();
            }
            
            Progress += delta;
            
            float duration = GetDuration();
            if (duration <= 0)
            {
                duration = 1f; // Default duration if no curves
            }
            
            if (Progress >= duration)
            {
                // Apply final state
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
            // 通过曲线末端（t=1）设置最终状态
            UITweenState finalState = new UITweenState();
            
            // 获取当前位置、Alpha 和 Size（用于不参与动画的属性）
            if (TargetTransform != null)
            {
                finalState.Position = TargetTransform.anchoredPosition;
                finalState.SizeX = TargetTransform.sizeDelta.x;
                finalState.SizeY = TargetTransform.sizeDelta.y;
            }
            if (GetTargetAlphaFunc != null)
            {
                finalState.Alpha = GetTargetAlphaFunc();
            }
            
            // 根据 PropType 和曲线末端（t=1）设置最终值
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                Vector2 pos = Config.MaxPos;
                if (Config.PosXCurve != null && Config.PosXCurve.length > 0)
                {
                    float curveValue = Config.PosXCurve.Evaluate(1f);
                    pos.x = Mathf.Lerp(Config.MinPos.x, Config.MaxPos.x, curveValue);
                }
                if (Config.PosYCurve != null && Config.PosYCurve.length > 0)
                {
                    float curveValue = Config.PosYCurve.Evaluate(1f);
                    pos.y = Mathf.Lerp(Config.MinPos.y, Config.MaxPos.y, curveValue);
                }
                finalState.Position = pos;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
            {
                float alpha = Config.MaxAlpha;
                if (Config.AlphaCurve != null && Config.AlphaCurve.length > 0)
                {
                    float curveValue = Config.AlphaCurve.Evaluate(1f);
                    alpha = Mathf.Lerp(Config.MinAlpha, Config.MaxAlpha, curveValue);
                }
                finalState.Alpha = alpha;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
            {
                float sizeX = Config.MaxSizeX;
                if (Config.SizeXCurve != null && Config.SizeXCurve.length > 0)
                {
                    float curveValue = Config.SizeXCurve.Evaluate(1f);
                    sizeX = Mathf.Lerp(Config.MinSizeX, Config.MaxSizeX, curveValue);
                }
                finalState.SizeX = sizeX;
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
            {
                float sizeY = Config.MaxSizeY;
                if (Config.SizeYCurve != null && Config.SizeYCurve.length > 0)
                {
                    float curveValue = Config.SizeYCurve.Evaluate(1f);
                    sizeY = Mathf.Lerp(Config.MinSizeY, Config.MaxSizeY, curveValue);
                }
                finalState.SizeY = sizeY;
            }
            
            ApplyState(finalState);
        }

        public void ApplyStateWithCurves(float currentProgress)
        {
            UITweenState state = new UITweenState();
            
            // 使用 Config.Duration（曲线时间范围是 0-1，相对于 Duration 归一化）
            float duration = GetDuration();
            
            // Normalize progress to 0-1 for curve evaluation（曲线时间范围是 0-1）
            float normalizedT = duration > 0 ? Mathf.Clamp01(currentProgress / duration) : 1f;
            
            if (((byte)PropType & (byte)UITweenPropType.Position) > 0)
            {
                Vector2 pos = StartState.Position;
                if (Config.PosXCurve != null && Config.PosXCurve.length > 0)
                {
                    // Evaluate curve at normalized time, curve value is interpolation factor (0-1)
                    float curveValue = Config.PosXCurve.Evaluate(normalizedT);
                    pos.x = Mathf.Lerp(StartState.Position.x, TargetState.Position.x, curveValue);
                }
                else
                {
                    pos.x = Mathf.Lerp(StartState.Position.x, TargetState.Position.x, normalizedT);
                }
                if (Config.PosYCurve != null && Config.PosYCurve.length > 0)
                {
                    float curveValue = Config.PosYCurve.Evaluate(normalizedT);
                    pos.y = Mathf.Lerp(StartState.Position.y, TargetState.Position.y, curveValue);
                }
                else
                {
                    pos.y = Mathf.Lerp(StartState.Position.y, TargetState.Position.y, normalizedT);
                }
                state.Position = pos;
            }
            if (((byte)PropType & (byte)UITweenPropType.Alpha) > 0)
            {
                if (Config.AlphaCurve != null && Config.AlphaCurve.length > 0)
                {
                    float curveValue = Config.AlphaCurve.Evaluate(normalizedT);
                    state.Alpha = Mathf.Lerp(StartState.Alpha, TargetState.Alpha, curveValue);
                }
                else
                {
                    state.Alpha = Mathf.Lerp(StartState.Alpha, TargetState.Alpha, normalizedT);
                }
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeX) > 0)
            {
                if (Config.SizeXCurve != null && Config.SizeXCurve.length > 0)
                {
                    float curveValue = Config.SizeXCurve.Evaluate(normalizedT);
                    state.SizeX = Mathf.Lerp(StartState.SizeX, TargetState.SizeX, curveValue);
                }
                else
                {
                    state.SizeX = Mathf.Lerp(StartState.SizeX, TargetState.SizeX, normalizedT);
                }
            }
            if (((byte)PropType & (byte)UITweenPropType.SizeY) > 0)
            {
                if (Config.SizeYCurve != null && Config.SizeYCurve.length > 0)
                {
                    float curveValue = Config.SizeYCurve.Evaluate(normalizedT);
                    state.SizeY = Mathf.Lerp(StartState.SizeY, TargetState.SizeY, curveValue);
                }
                else
                {
                    state.SizeY = Mathf.Lerp(StartState.SizeY, TargetState.SizeY, normalizedT);
                }
            }
            
            ApplyState(state);
        }

        public float GetDuration()
        {
            // 直接使用 Config.Duration（曲线时间范围是 0-1，相对于 Duration 归一化）
            if (Config.Duration > 0)
            {
                return Config.Duration;
            }
            return 1f; // Default duration if not set
        }
        
        /// <summary>
        /// 获取当前进度（0-1）
        /// </summary>
        public float GetProgress()
        {
            float duration = GetDuration();
            if (duration > 0)
            {
                return Mathf.Clamp01(Progress / duration);
            }
            return 1f; // 如果 duration <= 0，返回 1（已完成）
        }

        public void ToLastFrame()
        {
            ApplyFinalState();
        }
    }

}
