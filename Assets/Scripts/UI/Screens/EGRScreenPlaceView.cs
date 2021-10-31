using TMPro;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenPlaceView : EGRScreen, IEGRScreenSupportsBackKey {
        RawImage m_Cover;
        TextMeshProUGUI m_Name;
        TextMeshProUGUI m_Tags;
        TextMeshProUGUI m_Address;
        TextMeshProUGUI m_Description;
        RawImage m_MapPreview;
        EGRPlace m_Place;
        readonly MRKSelfContainedPtr<EGRUIUsableLoading> m_MapPreviewLoading;

        public EGRScreenPlaceView() {
            m_MapPreviewLoading = new MRKSelfContainedPtr<EGRUIUsableLoading>(
                () => (EGRUIUsableLoading)m_MapPreview.transform.parent.GetComponent<EGRUIUsableReference>().GetUsableIntitialized()
            );
        }

        protected override void OnScreenInit() {
            m_Cover = Body.GetElement<RawImage>("Cover/Image");
            m_Name = Body.GetElement<TextMeshProUGUI>("Layout/Name");
            m_Tags = Body.GetElement<TextMeshProUGUI>("Layout/Tags");
            m_Address = Body.GetElement<TextMeshProUGUI>("Layout/Address");
            m_Description = Body.GetElement<TextMeshProUGUI>("Layout/Desc/Text");
            m_MapPreview = Body.GetElement<RawImage>("Layout/MapPreview/Content/Texture");

            Body.GetElement<Button>("Back").onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShow() {
            //load map preview?
            LoadMapPreview();
        }

        protected override void OnScreenHide() {
            //clear map preview and recycle tex
            m_MapPreview.texture = null;

            m_MapPreviewLoading.Value.gameObject.SetActive(false);
        }

        void OnBackClick() {
            HideScreen();
        }

        public void SetPlace(EGRPlace place) {
            m_Place = place;
            m_Name.text = place.Name;
            m_Tags.text = place.Types.StringifyArray(", ");
            m_Address.text = place.Address;
            m_Description.text = place.Type;
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }

        void LoadMapPreview() {
            if (m_Place == null) {
                m_MapPreview.texture = null;
                return;
            }

            m_MapPreviewLoading.Value.gameObject.SetActive(true);

            MRKTileID tileID = MRKMapUtils.CoordinateToTileId(new Vector2d(m_Place.Latitude, m_Place.Longitude), 17);
            Client.Runnable.Run(MRKTileRequestor.Instance.RequestTile(tileID, false, OnReceivedMapPreviewResponse));
        }

        void OnReceivedMapPreviewResponse(MRKTileFetcherContext ctx) {
            m_MapPreviewLoading.Value.gameObject.SetActive(false);

            if (ctx.Error) {
                MRKLogger.LogError("Cannot load map preview");
                return;
            }

            if (ctx.Texture != null) {
                m_MapPreview.texture = ctx.MonitoredTexture.Value.Texture;
            }
        }
    }
}
