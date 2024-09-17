using MRK.Events;
using UnityEngine;

namespace MRK.UI.MapInterface
{
    public enum MapButtonEffectorType
    {
        Default,
        Centered
    }

    public class MapButtonEffector
    {
        public MapButton MapButton
        {
            get; private set;
        }
        public MapButtonGroup Group
        {
            get; private set;
        }

        public virtual MapButtonEffectorType EffectorType
        {
            get
            {
                return MapButtonEffectorType.Default;
            }
        }

        public void Initialize(MapButton button)
        {
            MapButton = button;
            Group = button.Group;

            button.Behaviour.EventManager.Register<UIMapButtonGroupExpansionStateChanged>(OnParentGroupExpansionStateChanged);
        }

        public void Destroy()
        {
            MapButton.Behaviour.EventManager.Unregister<UIMapButtonGroupExpansionStateChanged>(OnParentGroupExpansionStateChanged);
        }

        public void OnParentGroupExpansionStateChanged(UIMapButtonGroupExpansionStateChanged evt)
        {
            if (evt.Group == Group)
            {
                OnExpansionStateChanged(evt.Expanded);
            }
        }

        protected virtual void OnExpansionStateChanged(bool expanded)
        {
            //default effector behaviour
            //change sprite size
            float targetSpriteSize = expanded ? 140f : 100f;
            float oldSize = expanded ? 100f : 140f;
            Tweener.Tween(0.6f, (progress) =>
            {
                float deltaSize = Mathf.Lerp(oldSize, targetSpriteSize, progress);
                MapButton.SetSpriteSize(deltaSize, deltaSize);
            });
        }
    }
}
