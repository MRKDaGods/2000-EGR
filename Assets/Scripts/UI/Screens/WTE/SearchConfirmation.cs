using DG.Tweening;
using MRK.Networking.Packets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MRK.Localization;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class SearchConfirmation : BaseBehaviour
    {
        public class WTEContext
        {
            public string People;
            public int Price;
            public string PriceStr;
            public string Cuisine;
        }

        [SerializeField]
        private Button _searchButton;
        [SerializeField]
        private Button _backButton;
        [SerializeField]
        private TextMeshProUGUI _text;
        private WTEContext _context;

        private void Start()
        {
            _backButton.onClick.AddListener(Hide);
            _searchButton.onClick.AddListener(Search);

            rectTransform.anchoredPosition = new Vector2(rectTransform.rect.width, 0f); //position us right beside SSVM
        }

        private void OnValidate()
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.rect.width, 0f);
        }

        public void Show(WTEContext ctx)
        {
            _context = ctx;

            _text.text = string.Format(
                Localize(LanguageData.SEARCHING_FOR__color_orange__b__size_80__0___size___b___color__RESTAUARANTS_n_nFOR__color_orange__b__size_80__1___size___b___color__PEOPLE_n_nWITH_A_BUDGET_OF__color_orange__b__size_80__2_EGP__size___b___color_),
                ctx.Cuisine,
                ctx.People,
                ctx.PriceStr
            );

            rectTransform.DOAnchorPosX(0f, 0.3f);
        }

        public void Hide()
        {
            rectTransform.DOAnchorPosX(rectTransform.rect.width, 0.3f);
        }

        private void Search()
        {
            MessageBox msgBox = ScreenManager.MessageBox;

            if (!NetworkingClient.MainNetworkExternal.WTEQuery(byte.Parse(_context.People), _context.Price, _context.Cuisine, OnNetSearch))
            {
                msgBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    string.Format(Localize(LanguageData.FAILED__EGR__0__),
                    EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    null
                );

                return;
            }

            msgBox.ShowButton(false);
            msgBox.ShowPopup(
                Localize(LanguageData.WTE),
                Localize(LanguageData.SEARCHING___),
                null,
                null
            );
        }

        private void OnNetSearch(PacketInWTEQuery response)
        {
            MessageBox msgBox = ScreenManager.MessageBox;

            msgBox.HideScreen(() =>
            {
                if (response.Places.Count == 0)
                {
                    msgBox.ShowPopup(
                        Localize(LanguageData.WTE),
                        Localize(LanguageData.NO_RESTAURANTS_WERE_FOUND_MATCHING_THE_SPECIFIED_CRITERIA),
                        (x, y) => Hide(),
                        null
                    );

                    return;
                }

                PlaceList placeList = ScreenManager.GetScreen<PlaceList>();
                placeList.ShowScreen();
                placeList.SetPlaces(response.Places);
            }, response.Places.Count == 0 ? 1.1f : 0f, immediateSensitivtyCheck: true);
        }
    }
}
