using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MRK.UI {
    public class EGRScreenOptionsAdvancedSettings : EGRScreenAnimatedLayout {
        EGRUIMultiSelectorSettings m_InputModelSelector;
        TextMeshProUGUI m_Latitude;
        TextMeshProUGUI m_Longitude;
        TextMeshProUGUI m_Bearing;
        TextMeshProUGUI m_LastError;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(() => HideScreen());

            m_InputModelSelector = GetElement<EGRUIMultiSelectorSettings>("InputModelSelector");

            m_Latitude = GetElement<TextMeshProUGUI>("Layout/Latitude/Text");
            m_Longitude = GetElement<TextMeshProUGUI>("Layout/Longitude/Text");
            m_Bearing = GetElement<TextMeshProUGUI>("Layout/Bearing/Text");
            m_LastError = GetElement<TextMeshProUGUI>("Layout/Error/Text");
            GetElement<Button>("Layout/Request").onClick.AddListener(OnRequestLocation);
        }

        protected override void OnScreenShow() {
            m_InputModelSelector.SelectedIndex = (int)EGRSettings.InputModel;
            m_Latitude.text = m_Longitude.text = m_Bearing.text = m_LastError.text = "";
        }

        protected override void OnScreenHide() {
            EGRSettings.InputModel = (EGRSettingsInputModel)m_InputModelSelector.SelectedIndex;
            EGRSettings.Save();
        }

        void OnRequestLocation() {
            Client.LocationService.GetCurrentLocation(OnReceiveLocation);
        }

        void OnReceiveLocation(bool success, Vector2d? coord, float? bearing) {
            m_Latitude.text = success ? coord.Value.x.ToString() : "-";
            m_Longitude.text = success ? coord.Value.y.ToString() : "-";
            m_Bearing.text = success ? bearing.Value.ToString() : "-";
            m_LastError.text = Client.LocationService.LastError.ToString();
        }
    }
}
