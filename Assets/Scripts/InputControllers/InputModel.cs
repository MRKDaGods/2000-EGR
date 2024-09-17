using DG.Tweening;
using System;
using UnityEngine;

namespace MRK.InputControllers
{
    public abstract class InputModel
    {
        private static TweenModel _modelTween;
        private static MRKModel _modelMRK;

        public virtual bool NeedsUpdate
        {
            get
            {
                return false;
            }
        }

        public abstract void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set);

        public abstract void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set);

        public abstract void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set);

        public virtual void UpdateInputModel()
        {
        }

        public static InputModel Get(SettingsInputModel model)
        {
            switch (model)
            {
                case SettingsInputModel.Tween:
                    if (_modelTween == null)
                    {
                        _modelTween = new TweenModel();
                    }

                    return _modelTween;

                case SettingsInputModel.MRK:
                    if (_modelMRK == null)
                    {
                        _modelMRK = new MRKModel();
                    }

                    return _modelMRK;
            }

            return null;
        }
    }

    public class TweenModel : InputModel
    {
        private object _zoomTween;
        private object _panTweenLat;
        private object _panTweenLng;
        private object _rotationTweenX;
        private object _rotationTweenY;
        private object _rotationTweenZ;

        public override void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set)
        {
            if (_panTweenLat != null)
            {
                DOTween.Kill(_panTweenLat);
            }

            if (_panTweenLng != null)
            {
                DOTween.Kill(_panTweenLng);
            }

            _panTweenLat = DOTween.To(() => get().x, x => set(new Vector2d(x, get().y)), target.x, 0.5f)
                .SetEase(Ease.OutSine);

            _panTweenLng = DOTween.To(() => get().y, x => set(new Vector2d(get().x, x)), target.y, 0.5f)
                .SetEase(Ease.OutSine);
        }

        public override void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set)
        {
            if (_zoomTween != null)
            {
                DOTween.Kill(_zoomTween);
            }

            _zoomTween = DOTween.To(() => get(), x => set(x), target, 0.4f)
                .SetEase(Ease.OutSine);
        }

        public override void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set)
        {
            if (_rotationTweenX != null)
            {
                DOTween.Kill(_panTweenLat);
            }

            if (_rotationTweenY != null)
            {
                DOTween.Kill(_panTweenLng);
            }

            _rotationTweenX = DOTween.To(() => get().x, x => set(new Vector3(x, get().y, get().z)), target.x, 0.5f)
               .SetEase(Ease.OutSine);

            _rotationTweenY = DOTween.To(() => get().y, x => set(new Vector3(get().x, x, get().z)), target.y, 0.5f)
                .SetEase(Ease.OutSine);

            _rotationTweenZ = DOTween.To(() => get().z, x => set(new Vector3(get().x, get().y, x)), target.z, 0.5f)
                .SetEase(Ease.OutSine);
        }
    }

    public class MRKModel : InputModel
    {
        public struct Context<T> where T : struct
        {
            public Func<T> Get;
            public Action<T> Set;
            public T? Target;
            public float LastProcessTime;

            public bool CanUpdate
            {
                get
                {
                    return Get != null && Set != null && Target.HasValue && Time.time - LastProcessTime <= 1.5f;
                }
            }
        }

        private Context<float> _zoom;
        private Context<Vector2d> _pan;
        private Context<Vector3> _rotation;

        public Context<float> ZoomContext
        {
            get
            {
                return _zoom;
            }
        }

        public override bool NeedsUpdate
        {
            get
            {
                return _zoom.CanUpdate || _pan.CanUpdate || _rotation.CanUpdate;
            }
        }

        public override void ProcessPan(ref Vector2d current, ref Vector2d target, Func<Vector2d> get, Action<Vector2d> set)
        {
            _pan.Get = get;
            _pan.Set = set;
            _pan.Target = target;
            _pan.LastProcessTime = Time.time;
        }

        public override void ProcessZoom(ref float current, ref float target, Func<float> get, Action<float> set)
        {
            _zoom.Get = get;
            _zoom.Set = set;
            _zoom.Target = target;
            _zoom.LastProcessTime = Time.time;
        }

        public override void ProcessRotation(ref Vector3 current, ref Vector3 target, Func<Vector3> get, Action<Vector3> set)
        {
            _rotation.Get = get;
            _rotation.Set = set;
            _rotation.Target = target;
            _rotation.LastProcessTime = Time.time;
        }

        public override void UpdateInputModel()
        {
            if (_zoom.CanUpdate)
            {
                float current = _zoom.Get();

                current += (_zoom.Target.Value - current) * Time.deltaTime * 7f;
                _zoom.Set(current);
            }

            if (_pan.CanUpdate)
            {
                Vector2d current = _pan.Get();

                current += (_pan.Target.Value - current) * Time.deltaTime * 7f;
                _pan.Set(current);
            }

            if (_rotation.CanUpdate)
            {
                Vector3 current = _rotation.Get();

                current += (_rotation.Target.Value - current) * Time.deltaTime * 7f;
                _rotation.Set(current);
            }
        }
    }
}
