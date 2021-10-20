using TMPro;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenPlaceView : EGRScreen, IEGRScreenSupportsBackKey {
        RawImage m_Cover;
        TextMeshProUGUI m_Name;
        TextMeshProUGUI m_Tags;
        TextMeshProUGUI m_Address;
        EGRPlace m_Place;

        protected override void OnScreenInit() {
            m_Cover = Body.GetElement<RawImage>("Cover/Image");
            m_Name = Body.GetElement<TextMeshProUGUI>("Name");
            m_Tags = Body.GetElement<TextMeshProUGUI>("Tags");
            m_Address = Body.GetElement<TextMeshProUGUI>("Address/Text");

            Body.GetElement<Button>("Back").onClick.AddListener(OnBackClick);
        }

        void OnBackClick() {
            HideScreen();
        }

        public void SetPlace(EGRPlace place) {
            m_Place = place;
            m_Name.text = place.Name;
            m_Tags.text = place.Types.StringifyArray(", ");
            m_Address.text = place.Address;
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
