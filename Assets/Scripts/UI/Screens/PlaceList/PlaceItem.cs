using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.LanguageManager;

namespace MRK.UI {
    public partial class PlaceList {
        class PlaceItem : BaseBehaviourPlain {
            Transform m_Transform;
            RawImage m_Image;
            TextMeshProUGUI m_Name;
            TextMeshProUGUI m_Tags;
            bool m_Stationary;
            ulong? m_PlaceCID;

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

            public void SetInfo(string name, string tags, ulong? placeCID = null, Texture2D img = null) {
                Name = name;
                m_PlaceCID = placeCID;

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
                if (!m_PlaceCID.HasValue) {
                    MRKLogger.LogError($"{Name} has no CID");
                    return;
                }

                EGRPlace place = Client.PlaceManager.GetPlaceCached(m_PlaceCID.Value);
                if (place != null) {
                    OpenPlaceView(place);
                    return;
                }

                MessageBox msgBox = ScreenManager.MessageBox;
                msgBox.ShowButton(false);
                msgBox.ShowPopup(
                    Localize(LanguageData.EGR),
                    Localize(LanguageData.FETCHING_PLACE_DATA___),
                    null,
                    null
                );

                Client.PlaceManager.FetchPlace(m_PlaceCID.Value, (place) => {
                    msgBox.HideScreen(() => {
                        //an error has occured
                        if (place == null) {
                            msgBox.ShowPopup(
                                Localize(LanguageData.ERROR),
                                string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_RESPONSE),
                                null,
                                null
                            );

                            return;
                        }

                        OpenPlaceView(place);
                    }, 1.1f);
                });
            }

            void OpenPlaceView(EGRPlace place) {
                if (place == null) {
                    MRKLogger.LogError("Opening place view with null place !!");
                    return;
                }

                PlaceView placeView = ScreenManager.GetScreen<PlaceView>();
                placeView.SetPlace(place);
                placeView.ShowScreen();
            }
        }
    }
}
