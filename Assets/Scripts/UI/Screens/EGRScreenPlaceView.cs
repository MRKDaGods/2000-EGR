using TMPro;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenPlaceView : EGRScreen, IEGRScreenSupportsBackKey {
        RawImage m_Cover;
        TextMeshProUGUI m_Name;
        TextMeshProUGUI m_Tags;
        EGRPlace m_Place;

        protected override void OnScreenInit() {
            m_Cover = Body.GetElement<RawImage>("Cover/Image");
            m_Name = Body.GetElement<TextMeshProUGUI>("Name");
            m_Tags = Body.GetElement<TextMeshProUGUI>("Tags");

            Body.GetElement<Button>("Back").onClick.AddListener(OnBackClick);
        }

        void OnBackClick() {
            HideScreen();
        }

        public void SetPlace(EGRPlace place) {
            m_Place = place;
            m_Name.text = place.Name;
            m_Tags.text = place.Types.StringifyArray(", ");
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
