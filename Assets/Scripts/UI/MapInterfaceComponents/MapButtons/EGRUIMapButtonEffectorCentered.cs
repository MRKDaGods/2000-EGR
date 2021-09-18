using UnityEngine;

namespace MRK.UI.MapInterface {
    public class EGRUIMapButtonEffectorCentered : EGRUIMapButtonEffector {
        public override EGRUIMapButtonEffectorType EffectorType => EGRUIMapButtonEffectorType.Centered;

        protected override void OnExpansionStateChanged(bool expanded) {
            // resize sprite
            base.OnExpansionStateChanged(expanded);

            //fade text opacity
            float targetAlpha = expanded ? 1f : 0f;
            float oldAlpha = expanded ? 0f : 1f;
            MRKTweener.Tween(0.6f,(progress) => {
                float deltaAlpha = Mathf.Lerp(oldAlpha, targetAlpha, progress);
                MapButton.SetTextOpacity(deltaAlpha);
            },
            () => {
                MapButton.SetTextActive(expanded);
            });
        }
    }
}
