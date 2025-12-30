using System.Collections.Generic;

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

        public uint StartTween(UITweenEntry entry, UITweenObject obj, int loopTimes = 1)
        {
            UITween tween = PrepareTween(obj, loopTimes);
            tween.Tweens.Add(entry);
            return tween.Id;
        }

        public uint StartTween(IList<UITweenEntry> entries, UITweenObject obj, int loopTimes = 1)
        {
            UITween tween = PrepareTween(obj, loopTimes);
            tween.Tweens.AddRange(entries);
            return tween.Id;
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
            tween.TargetTweenIndex = -1;
            tween.TargetTransform = obj.TargetTransform;
            tween.GetTargetAlphaFunc = obj.GetTargetAlphaFunc;
            tween.SetTargetAlphaFunc = obj.SetTargetAlphaFunc;
            tween.OnTweenFinished = obj.OnTweenFinished;
            Tweens.Add(tween);
            if (Tweens.Count == 1)
            {
                TickingManager.Register(this);
            }
            NextTweenId ++;
            return tween;
        }

    }

}