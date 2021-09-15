using UnityEngine;

namespace MRK.UI.MapInterface {
    public enum EGRUIMapButtonEffectorType {
        Default,
        Centered
    }

    public class EGRUIMapButtonEffector {
        public EGRUIMapButton MapButton { get; private set; }
        public EGRUIMapButtonsGroup Group { get; private set; }

        public virtual EGRUIMapButtonEffectorType EffectorType => EGRUIMapButtonEffectorType.Default;

        public void Initialize(EGRUIMapButton button) {
            MapButton = button;
            Group = button.Group;

            button.Behaviour.EventManager.Register<EGREventUIMapButtonGroupExpansionStateChanged>(OnParentGroupExpansionStateChanged);
        }

        public void Destroy() {
            MapButton.Behaviour.EventManager.Unregister<EGREventUIMapButtonGroupExpansionStateChanged>(OnParentGroupExpansionStateChanged);
        }

        public void OnParentGroupExpansionStateChanged(EGREventUIMapButtonGroupExpansionStateChanged evt) {
            if (evt.Group == Group) {
                OnExpansionStateChanged(evt.Expanded);
            }
        }

        protected virtual void OnExpansionStateChanged(bool expanded) {
            //default effector behaviour
            //change sprite size
            float targetSpriteSize = expanded ? 140f : 100f;
            float oldSize = expanded ? 100f : 140f;
            MRKTweener.Tween(0.6f, (progress) => {
                float deltaSize = Mathf.Lerp(oldSize, targetSpriteSize, progress);
                MapButton.SetSpriteSize(deltaSize, deltaSize);
            });
        }
    }
}
