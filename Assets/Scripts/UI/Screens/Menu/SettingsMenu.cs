using DG.Tweening;
using MRK.Localization;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.Localization.LanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_MenuSettings;

namespace MRK.UI
{
    public class SettingsMenu : Screen
    {
        private MultiSelector _quality;
        private MultiSelector _fps;
        private SegmentedControl _showTime;
        private SegmentedControl _showDist;
        private bool _graphicsModified;

        protected override void OnScreenInit()
        {
            GetElement<Button>(Buttons.Back).onClick.AddListener(OnBackClick);

            //we gotta do it manually *shrug*
            _quality = GetElement<MultiSelector>("LAYOUT/Quality/Custom");
            _fps = GetElement<MultiSelector>("LAYOUT/FPS/Custom");
            _showTime = GetElement<SegmentedControl>("LAYOUT/Time/Segmented");
            _showDist = GetElement<SegmentedControl>("LAYOUT/Distance/Segmented");
        }

        protected override void OnScreenShow()
        {
            //set values from settings
            _quality.SelectedIndex = (int)Settings.Quality;
            _fps.SelectedIndex = (int)Settings.FPS;
            _showTime.selectedSegmentIndex = Settings.ShowTime ? 0 : 1; //SHOW = 0, HIDE = 1 based on hierarchy
            _showDist.selectedSegmentIndex = Settings.ShowDistance ? 0 : 1;
        }

        private void OnBackClick()
        {
            if ((SettingsQuality)_quality.SelectedIndex != Settings.Quality || (SettingsFPS)_fps.SelectedIndex != Settings.FPS)
            {
                _graphicsModified = true;

                Confirmation popup = ScreenManager.GetPopup<Confirmation>();
                popup.SetYesButtonText(Localize(LanguageData.APPLY));
                popup.SetNoButtonText(Localize(LanguageData.CANCEL));
                popup.ShowPopup(Localize(LanguageData.SETTINGS), Localize(LanguageData.GRAPHIC_SETTINGS_WERE_MODIFIED_nWOULD_YOU_LIKE_TO_APPLY_THEM_), OnUnsavedClose, null);
                return;
            }

            HideScreen();
        }

        private void OnUnsavedClose(Popup popup, PopupResult result)
        {
            if (result == PopupResult.YES)
            {
                Settings.Quality = (SettingsQuality)_quality.SelectedIndex;
                Settings.FPS = (SettingsFPS)_fps.SelectedIndex;
            }

            HideScreen();
        }

        protected override void OnScreenHide()
        {
            Settings.Save();

            if (_graphicsModified)
                Settings.Apply();
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(_lastGraphicsBuf, (x, y) =>
            {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.1f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(_lastGraphicsBuf, (x, y) =>
            {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.05f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
