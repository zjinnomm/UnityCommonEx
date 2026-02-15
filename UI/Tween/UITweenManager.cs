using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public class UITweenManager : Singleton<UITweenManager>, ITickable
    {

        uint NextTweenId = 1;
        List<UITween> Tweens = new List<UITween>();

        public bool IsTicking() => Tweens.Count > 0;

        public void Tick(float delta)
        {
            for (int i = Tweens.Count - 1; i >= 0; i--)
            {
                UITween tween = Tweens[i];
                bool finished = tween.TickTween(delta);
                if (finished)
                {
                    Tweens.RemoveAt(i);
                    tween.OnTweenFinished?.Invoke();
                    InstancePool<UITween>.Instance.ReturnInstance(tween);
                }
            }
        }

        public uint StartTween(UITweenObject obj, UITweenConfig config, float startProgress = 0f, int loopTimes = 1)
        {
            UITween tween = PrepareTween(obj, loopTimes);
            tween.Config = config;
            tween.PropType = config.PropType;
            tween.IsActive = true;

            if (startProgress > 0f)
            {
                float duration = tween.GetDuration();
                if (duration > 0)
                {
                    tween.Progress = Mathf.Clamp01(startProgress) * duration;
                    tween.PrepareTween();
                    tween.ApplyStateWithCurves(tween.Progress);
                }
            }
            else
            {
                // 立即应用 t=0 的初始状态，避免回收复用时首帧仍显示上一轮 Tween 结束位置
                tween.PrepareTween();
            }

            return tween.Id;
        }
        
        /// <summary>
        /// 获取 Tween 的当前进度（0-1），查不到或已结束返回 1
        /// </summary>
        public float GetTweenProgress(uint id)
        {
            for (int i = 0; i < Tweens.Count; i++)
            {
                UITween tween = Tweens[i];
                if (tween.Id == id)
                {
                    return tween.GetProgress();
                }
            }
            return 1f; // 查不到说明已结束，返回 1
        }

        public void StopTween(uint id, bool toLastFrame = true)
        {
            for (int i = 0; i < Tweens.Count; i++)
            {
                UITween tween = Tweens[i];
                if (tween.Id == id)
                {
                    Tweens.RemoveAt(i);
                    if (toLastFrame)
                    {
                        tween.ToLastFrame();
                    }
                    tween.OnTweenFinished?.Invoke();
                    InstancePool<UITween>.Instance.ReturnInstance(tween);
                    return;
                }
            }
        }

        UITween PrepareTween(UITweenObject obj, int loopTimes)
        {
            UITween tween = InstancePool<UITween>.Instance.GetInstance();
            tween.Id = NextTweenId;
            tween.LoopTimes = loopTimes;
            tween.TargetTransform = obj.TargetTransform;
            tween.GetTargetAlphaFunc = obj.GetTargetAlphaFunc;
            tween.SetTargetAlphaFunc = obj.SetTargetAlphaFunc;
            tween.OnTweenFinished = obj.OnTweenFinished;
            Tweens.Add(tween);
            if (Tweens.Count == 1)
            {
                TickingManager.Register(this);
            }
            NextTweenId++;
            return tween;
        }

    }

}
