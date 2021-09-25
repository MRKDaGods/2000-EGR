using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenOptionsMapSettings : EGRScreenAnimatedLayout, IEGRScreenSupportsBackKey {
        EGRUIMultiSelectorSettings m_SensitivitySelector;
        EGRUIMultiSelectorSettings m_StyleSelector;
        EGRUIMultiSelectorSettings m_AngleSelector;

        protected override string LayoutPath => "Scroll View/Viewport/Content/Layout";
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);
            GetElement<Button>($"{LayoutPath}/Preview").onClick.AddListener(OnPreviewClick);

            m_SensitivitySelector = GetElement<EGRUIMultiSelectorSettings>("SensitivitySelector");
            m_StyleSelector = GetElement<EGRUIMultiSelectorSettings>("StyleSelector");
            m_AngleSelector = GetElement<EGRUIMultiSelectorSettings>("AngleSelector");

            GetElement<Button>($"{LayoutPath}/DeleteCache").onClick.AddListener(OnDeleteCacheClick);
        }

        protected override void OnScreenShow() {
            m_SensitivitySelector.SelectedIndex = (int)EGRSettings.MapSensitivity;
            m_StyleSelector.SelectedIndex = (int)EGRSettings.MapStyle;
            m_AngleSelector.SelectedIndex = (int)EGRSettings.MapViewingAngle;
        }

        protected override void OnScreenHide() {
            EGRSettings.MapSensitivity = (EGRSettingsSensitivity)m_SensitivitySelector.SelectedIndex;

            EGRSettingsMapStyle newStyle = (EGRSettingsMapStyle)m_StyleSelector.SelectedIndex;
            bool styleChanged = newStyle != EGRSettings.MapStyle;
            EGRSettings.MapStyle = newStyle;

            EGRSettings.MapViewingAngle = (EGRSettingsMapViewingAngle)m_AngleSelector.SelectedIndex;

            EGRSettings.Save();

            Client.FlatMap.UpdateTileset();

            if (styleChanged) {
                MRKTileMonitor.Instance.DestroyLeaks();
            }

            if (Client.ActiveEGRCamera.InterfaceActive) {
                Client.ActiveEGRCamera.ResetStates();

                if (Client.MapMode == EGRMapMode.Flat) {
                    Client.FlatCamera.UpdateMapViewingAngles();
                }
            }
        }

        void OnPreviewClick() {
            EGRScreenMapChooser mapChooser = ScreenManager.GetScreen<EGRScreenMapChooser>();
            mapChooser.MapStyleCallback = OnMapStyleChosen;
            mapChooser.ShowScreen();
        }

        void OnMapStyleChosen(int style) {
            m_StyleSelector.SelectedIndex = style;
        }

        void OnDeleteCacheClick() {
            EGRPopupConfirmation popup = ScreenManager.GetPopup<EGRPopupConfirmation>();
            popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
            popup.ShowPopup(
                Localize(EGRLanguageData.EGR),
                Localize(EGRLanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_DELETE_THE_OFFLINE_MAP_CACHE_),
                (_, result) => {
                    if (result == EGRPopupResult.YES) {
                        DeleteLocalMapCache();
                    }
                },
                this);
        }

        void DeleteLocalMapCache() {
            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(EGRLanguageData.EGR),
                Localize(EGRLanguageData.DELETING_OFFLINE_MAP_CACHE___),
                null,
                this);

            Client.GlobalThreadPool.QueueTask(() => {
                MRKTileRequestor.Instance.DeleteLocalProvidersCache();

                Client.Runnable.RunOnMainThread(() => {
                    MessageBox.HideScreen(() => {
                        MessageBox.ShowPopup(
                            Localize(EGRLanguageData.EGR),
                            Localize(EGRLanguageData.OFFLINE_MAP_CACHE_HAS_BEEN_DELETED),
                            null,
                            this);
                    }, 1.1f);
                });
            });
        }

        void OnBackClick() {
            HideScreen();
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
