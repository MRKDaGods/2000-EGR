using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAudioSettings : EGRScreenAnimatedLayout, IEGRScreenSupportsBackKey {
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);
        }

        void OnBackClick() {
            HideScreen();
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
