using DG.Tweening;
using System;
using System.Collections.Generic;

namespace MRK {
    public class MRKTweener {
        class LocalTween {
            float m_Progress;
            Action<float> m_ProgressCallback;
            Action m_CompletionCallback;

            public LocalTween(Action<float> progressCallback, Action completionCallback) {
                m_ProgressCallback = progressCallback;
                m_CompletionCallback = completionCallback;
            }

            public float GetProgress() {
                return m_Progress;
            }

            public void SetProgress(float progress) {
                m_Progress = progress;
            }

            public void SetTween(Tween tween) {
                tween.OnUpdate(() => m_ProgressCallback(m_Progress));
                tween.OnComplete(() => {
                    m_CompletionCallback?.Invoke();
                    ms_Tweens.Remove(this);
                });
            }
        }

        readonly static HashSet<LocalTween> ms_Tweens;

        static MRKTweener() {
            ms_Tweens = new HashSet<LocalTween>();
        }

        public static void Tween(float duration, Action<float> progressCallback, Action completionCallback = null, Ease easing = Ease.OutSine) {
            if (progressCallback == null)
                return;

            LocalTween lt = new LocalTween(progressCallback, completionCallback);
            ms_Tweens.Add(lt);

            lt.SetTween(DOTween.To(lt.GetProgress, lt.SetProgress, 1f, duration).SetEase(easing));
        }
    }
}
