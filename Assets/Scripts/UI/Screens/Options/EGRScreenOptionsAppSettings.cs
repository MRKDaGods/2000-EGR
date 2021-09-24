using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAppSettings : EGRScreenAnimatedLayout {
        Image m_Background;

        protected override bool IsRTL => false;
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bTopLeftMenu").onClick.AddListener(() => {
                HideScreen(() => ScreenManager.GetScreen<EGRScreenMenu>().ShowScreen(), 0.1f, false);
            });

            GetElement<Button>("Layout/Display").onClick.AddListener(() => {
                ScreenManager.GetScreen<EGRScreenOptionsDisplaySettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Audio").onClick.AddListener(() => {
                ScreenManager.GetScreen<EGRScreenOptionsAudioSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Globe").onClick.AddListener(() => {
                ScreenManager.GetScreen<EGRScreenOptionsGlobeSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Map").onClick.AddListener(() => {
                ScreenManager.GetScreen<EGRScreenOptionsMapSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Advanced").onClick.AddListener(() => {
                ScreenManager.GetScreen<EGRScreenOptionsAdvancedSettings>().ShowScreen();
            });

            m_Background = GetElement<Image>("imgBg");
        }

        protected override bool CanAnimate(Graphic gfx, bool moving) {
            return !(moving && gfx == m_Background);
        }
    }
}
