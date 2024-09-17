using MRK.Localization;
using MRK.Maps;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class MapSettings : AnimatedLayout, ISupportsBackKey
    {
        private MultiSelectorSettings _sensitivitySelector;
        private MultiSelectorSettings _styleSelector;
        private MultiSelectorSettings _angleSelector;

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
            GetElement<Button>($"{LayoutPath}/Preview").onClick.AddListener(OnPreviewClick);

            _sensitivitySelector = GetElement<MultiSelectorSettings>("SensitivitySelector");
            _styleSelector = GetElement<MultiSelectorSettings>("StyleSelector");
            _angleSelector = GetElement<MultiSelectorSettings>("AngleSelector");

            GetElement<Button>($"{LayoutPath}/DeleteCache").onClick.AddListener(OnDeleteCacheClick);
        }

        protected override void OnScreenShow()
        {
            _sensitivitySelector.SelectedIndex = (int)Settings.MapSensitivity;
            _styleSelector.SelectedIndex = (int)Settings.MapStyle;
            _angleSelector.SelectedIndex = (int)Settings.MapViewingAngle;
        }

        protected override void OnScreenHide()
        {
            Settings.MapSensitivity = (SettingsSensitivity)_sensitivitySelector.SelectedIndex;

            SettingsMapStyle newStyle = (SettingsMapStyle)_styleSelector.SelectedIndex;
            bool styleChanged = newStyle != Settings.MapStyle;
            Settings.MapStyle = newStyle;

            Settings.MapViewingAngle = (SettingsMapViewingAngle)_angleSelector.SelectedIndex;

            Settings.Save();

            Client.FlatMap.UpdateTileset();

            if (styleChanged)
            {
                TileMonitor.Instance.DestroyLeaks();
            }

            if (Client.ActiveEGRCamera.InterfaceActive)
            {
                Client.ActiveEGRCamera.ResetStates();

                if (Client.MapMode == EGRMapMode.Flat)
                {
                    Client.FlatCamera.UpdateMapViewingAngles();
                }
            }
        }

        private void OnPreviewClick()
        {
            MapChooser mapChooser = ScreenManager.GetScreen<MapChooser>();
            mapChooser.MapStyleCallback = OnMapStyleChosen;
            mapChooser.ShowScreen();
        }

        private void OnMapStyleChosen(int style)
        {
            _styleSelector.SelectedIndex = style;
        }

        private void OnDeleteCacheClick()
        {
            Confirmation popup = ScreenManager.GetPopup<Confirmation>();
            popup.SetNoButtonText(Localize(LanguageData.CANCEL));
            popup.ShowPopup(
                Localize(LanguageData.EGR),
                Localize(LanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_DELETE_THE_OFFLINE_MAP_CACHE_),
                (_, result) =>
                {
                    if (result == PopupResult.YES)
                    {
                        DeleteLocalMapCache();
                    }
                },
                this);
        }

        private void DeleteLocalMapCache()
        {
            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(LanguageData.EGR),
                Localize(LanguageData.DELETING_OFFLINE_MAP_CACHE___),
                null,
                this);

            Client.GlobalThreadPool.QueueTask(() =>
            {
                TileRequestor.Instance.DeleteLocalProvidersCache();

                Client.Runnable.RunOnMainThread(() =>
                {
                    MessageBox.HideScreen(() =>
                    {
                        MessageBox.ShowPopup(
                            Localize(LanguageData.EGR),
                            Localize(LanguageData.OFFLINE_MAP_CACHE_HAS_BEEN_DELETED),
                            null,
                            this);
                    }, 1.1f);
                });
            });
        }

        private void OnBackClick()
        {
            HideScreen();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
