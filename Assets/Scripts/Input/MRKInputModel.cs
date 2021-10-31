using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public abstract class MRKInputModel {
        static MRKInputModelTween ms_ModelTween;
        static MRKInputModelMRK ms_ModelMRK;

        public virtual bool NeedsUpdate => false;

        public abstract void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set);

        public abstract void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set);

        public abstract void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set);

        public virtual void UpdateInputModel() {
        }

        public static MRKInputModel Get(EGRSettingsInputModel model) {
            switch (model) {

                case EGRSettingsInputModel.Tween:
                    if (ms_ModelTween == null)
                        ms_ModelTween = new MRKInputModelTween();

                    return ms_ModelTween;

                case EGRSettingsInputModel.MRK:
                    if (ms_ModelMRK == null)
                        ms_ModelMRK = new MRKInputModelMRK();

                    return ms_ModelMRK;

            }

            return null;
        }
    }

    public class MRKInputModelTween : MRKInputModel {
        object m_ZoomTween;
        object m_PanTweenLat;
        object m_PanTweenLng;
        object m_RotationTweenX;
        object m_RotationTweenY;
        object m_RotationTweenZ;

        public override void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set) {
            if (m_PanTweenLat != null) {
                DOTween.Kill(m_PanTweenLat);
            }

            if (m_PanTweenLng != null) {
                DOTween.Kill(m_PanTweenLng);
            }

            m_PanTweenLat = DOTween.To(() => get().x, x => set(new Vector2d(x, get().y)), target.x, 0.5f)
                .SetEase(Ease.OutSine);

            m_PanTweenLng = DOTween.To(() => get().y, x => set(new Vector2d(get().x, x)), target.y, 0.5f)
                .SetEase(Ease.OutSine);
        }

        public override void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set) {
            if (m_ZoomTween != null) {
                DOTween.Kill(m_ZoomTween);
            }

            m_ZoomTween = DOTween.To(() => get(), x => set(x), target, 0.4f)
                .SetEase(Ease.OutSine);
        }

        public override void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set) {
            if (m_RotationTweenX != null) {
                DOTween.Kill(m_PanTweenLat);
            }

            if (m_RotationTweenY != null) {
                DOTween.Kill(m_PanTweenLng);
            }

            m_RotationTweenX = DOTween.To(() => get().x, x => set(new Vector3(x, get().y, get().z)), target.x, 0.5f)
               .SetEase(Ease.OutSine);

            m_RotationTweenY = DOTween.To(() => get().y, x => set(new Vector3(get().x, x, get().z)), target.y, 0.5f)
                .SetEase(Ease.OutSine);

            m_RotationTweenZ = DOTween.To(() => get().z, x => set(new Vector3(get().x, get().y, x)), target.z, 0.5f)
                .SetEase(Ease.OutSine);
        }
    }

    public class MRKInputModelMRK : MRKInputModel {
        public struct Context<T> where T : struct {
            public Func<T> Get;
            public Action<T> Set;
            public T? Target;
            public float LastProcessTime;

            public bool CanUpdate => Get != null && Set != null && Target.HasValue && Time.time - LastProcessTime <= 1.5f;
        }

        Context<float> m_Zoom;
        Context<Vector2d> m_Pan;
        Context<Vector3> m_Rotation;

        public Context<float> ZoomContext => m_Zoom;

        public override bool NeedsUpdate => m_Zoom.CanUpdate || m_Pan.CanUpdate || m_Rotation.CanUpdate;

        public override void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set) {
            m_Pan.Get = get;
            m_Pan.Set = set;
            m_Pan.Target = target;
            m_Pan.LastProcessTime = Time.time;
        }

        public override void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set) {
            m_Zoom.Get = get;
            m_Zoom.Set = set;
            m_Zoom.Target = target;
            m_Zoom.LastProcessTime = Time.time;
        }

        public override void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set) {
            m_Rotation.Get = get;
            m_Rotation.Set = set;
            m_Rotation.Target = target;
            m_Rotation.LastProcessTime = Time.time;
        }

        public override void UpdateInputModel() {
            if (m_Zoom.CanUpdate) {
                float current = m_Zoom.Get();

                current += (m_Zoom.Target.Value - current) * Time.deltaTime * 7f;
                m_Zoom.Set(current);
            }

            if (m_Pan.CanUpdate) {
                Vector2d current = m_Pan.Get();

                current += (m_Pan.Target.Value - current) * Time.deltaTime * 7f;
                m_Pan.Set(current);
            }

            if (m_Rotation.CanUpdate) {
                Vector3 current = m_Rotation.Get();

                current += (m_Rotation.Target.Value - current) * Time.deltaTime * 7f;
                m_Rotation.Set(current);
            }
        }
    }
}
