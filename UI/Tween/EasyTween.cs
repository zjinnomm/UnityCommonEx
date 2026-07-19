using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// 挂在一个物体上，配置一个 Tween，激活后循环播放该 Tween；停用时停止播放。
    /// </summary>
    public class EasyTween : NodeController
    {
        /// <summary>
        /// 要循环播放的 Tween 配置。PropType 为 0 时不播放。
        /// </summary>
        public UITweenConfig Tween;

        /// <summary>
        /// 施加 Tween 的目标 RectTransform；不填则使用自身。
        /// </summary>
        public RectTransform TweenTarget;

        private uint _tweenId;

        private void OnEnable()
        {
            StartLoopTween();
        }

        private void OnDisable()
        {
            StopLoopTween();
        }

        private void StartLoopTween()
        {
            if (Tween.PropType == 0) return;
            if (!isActiveAndEnabled) return;

            RectTransform target = TweenTarget != null ? TweenTarget : transform as RectTransform;
            if (target == null) return;

            StopLoopTween();

            var tweenObj = new UITweenObject
            {
                TargetTransform = target,
                GetTargetAlphaFunc = null,
                SetTargetAlphaFunc = null,
                OnTweenFinished = null
            };

            _tweenId = UITweenManager.Instance.StartTween(tweenObj, Tween, 0f, int.MaxValue);
        }

        private void StopLoopTween()
        {
            if (_tweenId == 0) return;
            UITweenManager.Instance.StopTween(_tweenId, false);
            _tweenId = 0;
        }
    }
}
