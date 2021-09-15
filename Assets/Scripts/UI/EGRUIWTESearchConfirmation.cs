using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using static MRK.EGRLanguageManager;
using MRK.Networking.Packets;

namespace MRK.UI {
    public class EGRUIWTESearchConfirmation : MRKBehaviour {
        public class WTEContext {
            public string People;
            public int Price;
            public string PriceStr;
            public string Cuisine;
        }

        [SerializeField]
        Button m_SearchButton;
        [SerializeField]
        Button m_BackButton;
        [SerializeField]
        TextMeshProUGUI m_Text;
        WTEContext m_Context;

        RectTransform m_RectTransform => (RectTransform)transform;

        void Start() {
            m_BackButton.onClick.AddListener(Hide);
            m_SearchButton.onClick.AddListener(Search);

            m_RectTransform.anchoredPosition = new Vector2(m_RectTransform.rect.width, 0f); //position us right beside SSVM
        }

        void OnValidate() {
            m_RectTransform.anchoredPosition = new Vector2(m_RectTransform.rect.width, 0f);
        }

        public void Show(WTEContext ctx) {
            m_Context = ctx;

            m_Text.text = string.Format(Localize(
                EGRLanguageData.SEARCHING_FOR__color_orange__b__size_80__0___size___b___color__RESTAUARANTS_n_nFOR__color_orange__b__size_80__1___size___b___color__PEOPLE_n_nWITH_A_BUDGET_OF__color_orange__b__size_80__2_EGP__size___b___color_),
                ctx.Cuisine, ctx.People, ctx.PriceStr);

            m_RectTransform.DOAnchorPosX(0f, 0.3f);
        }

        public void Hide() {
            m_RectTransform.DOAnchorPosX(m_RectTransform.rect.width, 0.3f);
        }

        void Search() {
            EGRPopupMessageBox msgBox = ScreenManager.MessageBox;

            if (!NetworkingClient.MainNetworkExternal.WTEQuery(byte.Parse(m_Context.People), m_Context.Price, m_Context.Cuisine, OnNetSearch)) {
                msgBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, null);
                return;
            }

            msgBox.ShowButton(false);
            msgBox.ShowPopup(Localize(EGRLanguageData.WTE), Localize(EGRLanguageData.SEARCHING___), null, null);
        }

        void OnNetSearch(PacketInWTEQuery response) {
            EGRPopupMessageBox msgBox = ScreenManager.MessageBox;

            msgBox.HideScreen(() => {
                if (response.Places.Count == 0) {
                    msgBox.ShowPopup(Localize(EGRLanguageData.WTE), Localize(EGRLanguageData.NO_RESTAURANTS_WERE_FOUND_MATCHING_THE_SPECIFIED_CRITERIA), (x, y) => Hide(), null);
                    return;
                }

                EGRScreenPlaceList placeList = ScreenManager.GetScreen<EGRScreenPlaceList>();
                placeList.ShowScreen();
                placeList.SetPlaces(response.Places);
            }, 1.1f);
        }
    }
}
