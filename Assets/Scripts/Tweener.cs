using DG.Tweening;
using System;
using System.Collections.Generic;

namespace MRK
{
    public class Tweener
    {
        private class LocalTween
        {
            private float _progress;
            private readonly Action<float> _progressCallback;
            private readonly Action _completionCallback;

            public LocalTween(Action<float> progressCallback, Action completionCallback)
            {
                _progressCallback = progressCallback;
                _completionCallback = completionCallback;
            }

            public float GetProgress()
            {
                return _progress;
            }

            public void SetProgress(float progress)
            {
                _progress = progress;
            }

            public void SetTween(Tween tween)
            {
                tween.OnUpdate(() => _progressCallback(_progress));
                tween.OnComplete(() => {
                    _completionCallback?.Invoke();
                    _tweens.Remove(this);
                });
            }
        }

        private static readonly HashSet<LocalTween> _tweens;

        static Tweener()
        {
            _tweens = new HashSet<LocalTween>();
        }

        public static void Tween(float duration, Action<float> progressCallback, Action completionCallback = null, Ease easing = Ease.OutSine)
        {
            if (progressCallback == null)
            {
                return;
            }

            LocalTween lt = new LocalTween(progressCallback, completionCallback);
            _tweens.Add(lt);

            lt.SetTween(DOTween.To(lt.GetProgress, lt.SetProgress, 1f, duration).SetEase(easing));
        }
    }
}
