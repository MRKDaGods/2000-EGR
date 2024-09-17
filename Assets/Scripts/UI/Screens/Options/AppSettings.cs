using UnityEngine.UI;

namespace MRK.UI
{
    public class AppSettings : AnimatedLayout, ISupportsBackKey
    {
        private Image _background;

        protected override bool IsRTL
        {
            get
            {
                return false;
            }
        }

        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0xFF000000;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("bTopLeftMenu").onClick.AddListener(OnBackClick);

            GetElement<Button>("Layout/Display").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<DisplaySettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Audio").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<AudioSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Globe").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<GlobeSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Map").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<MapSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Advanced").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<AdvancedSettings>().ShowScreen();
            });

            _background = GetElement<Image>("imgBg");
        }

        protected override bool CanAnimate(Graphic gfx, bool moving)
        {
            return !(moving && gfx == _background);
        }

        private void OnBackClick()
        {
            HideScreen(() => ScreenManager.GetScreen<Menu>().ShowScreen(), 0.1f, false);
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
