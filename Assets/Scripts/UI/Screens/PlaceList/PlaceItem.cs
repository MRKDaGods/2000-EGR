using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public partial class EGRScreenPlaceList {
        class PlaceItem : MRKBehaviourPlain {
            Transform m_Transform;
            RawImage m_Image;
            TextMeshProUGUI m_Name;
            TextMeshProUGUI m_Tags;
            bool m_Stationary;

            public Transform Transform => m_Transform;
            public string Name { get; private set; }

            public PlaceItem(Transform transform, bool stationary = false) {
                m_Stationary = stationary;
                if (stationary)
                    return;

                m_Transform = transform;
                m_Image = transform.GetElement<RawImage>("Icon");
                m_Name = transform.GetElement<TextMeshProUGUI>("Name");
                m_Tags = transform.GetElement<TextMeshProUGUI>("Tags");

                transform.GetComponent<Button>().onClick.AddListener(OnButtonClick);
            }

            public void SetInfo(string name, string tags, Texture2D img = null) {
                Name = name;
                if (m_Stationary)
                    return;

                m_Name.text = name;
                m_Tags.text = tags;
                m_Image.texture = img;
            }

            public void SetActive(bool active) {
                m_Transform.gameObject.SetActive(active);
            }

            void OnButtonClick() {
                EGRScreenPlaceView placeView = ScreenManager.GetScreen<EGRScreenPlaceView>();
            }
        }
    }
}
