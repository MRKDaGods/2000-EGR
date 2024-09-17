using MRK.Localization;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class DisplaySettings : AnimatedLayout, ISupportsBackKey
    {
        private MultiSelectorSettings _qualitySelector;
        private MultiSelectorSettings _fpsSelector;
        private MultiSelectorSettings _resolutionSelector;
        private bool _graphicsModified;

        protected override string LayoutPath
        {
            get
            {
                return "Scroll View/Viewport/Content/Layout";
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

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            _qualitySelector = GetElement<MultiSelectorSettings>("QualitySelector");
            _fpsSelector = GetElement<MultiSelectorSettings>("FpsSelector");
            _resolutionSelector = GetElement<MultiSelectorSettings>("ResolutionSelector");
        }

        protected override void OnScreenShow()
        {
            _qualitySelector.SelectedIndex = (int)Settings.Quality;
            _fpsSelector.SelectedIndex = (int)Settings.FPS;
            _resolutionSelector.SelectedIndex = (int)Settings.Resolution;

            _graphicsModified = false;
        }

        protected override void OnScreenHide()
        {
            Settings.Save();

            if (_graphicsModified)
                Settings.Apply();
        }

        private void OnBackClick()
        {
            if ((SettingsQuality)_qualitySelector.SelectedIndex != Settings.Quality
                || (SettingsFPS)_fpsSelector.SelectedIndex != Settings.FPS
                || (SettingsResolution)_resolutionSelector.SelectedIndex != Settings.Resolution)
            {
                _graphicsModified = true;

                Confirmation popup = ScreenManager.GetPopup<Confirmation>();
                popup.SetYesButtonText(Localize(LanguageData.APPLY));
                popup.SetNoButtonText(Localize(LanguageData.CANCEL));
                popup.ShowPopup(
                    Localize(LanguageData.SETTINGS),
                    Localize(LanguageData.GRAPHIC_SETTINGS_WERE_MODIFIED_nWOULD_YOU_LIKE_TO_APPLY_THEM_),
                    OnUnsavedClose,
                    null
                );

                return;
            }

            HideScreen();
        }

        private void OnUnsavedClose(Popup popup, PopupResult result)
        {
            if (result == PopupResult.YES)
            {
                Settings.Quality = (SettingsQuality)_qualitySelector.SelectedIndex;
                Settings.FPS = (SettingsFPS)_fpsSelector.SelectedIndex;
                Settings.Resolution = (SettingsResolution)_resolutionSelector.SelectedIndex;
            }

            HideScreen();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
