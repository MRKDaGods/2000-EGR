using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public partial class EGRScreenPlaceList {
        class PlaceItem {
            Transform m_Transform;
            RawImage m_Image;
            TextMeshProUGUI m_Name;
            TextMeshProUGUI m_Tags;

            public PlaceItem(Transform transform) {
                m_Transform = transform;
                m_Image = transform.GetElement<RawImage>("Icon");
                m_Name = transform.GetElement<TextMeshProUGUI>("Name");
                m_Tags = transform.GetElement<TextMeshProUGUI>("Tags");
            }

            public void SetInfo(string name, string tags, Texture2D img = null) {
                m_Name.text = name;
                m_Tags.text = tags;
                m_Image.texture = img;
            }

            public void SetActive(bool active) {
                m_Transform.gameObject.SetActive(active);
            }
        }
    }
}
