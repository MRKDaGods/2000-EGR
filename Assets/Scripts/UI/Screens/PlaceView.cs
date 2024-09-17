using MRK.Maps;
using TMPro;
using UnityEngine.UI;

namespace MRK.UI
{
    public class PlaceView : Screen, ISupportsBackKey
    {
        private RawImage _cover;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _tags;
        private TextMeshProUGUI _address;
        private TextMeshProUGUI _description;
        private RawImage _mapPreview;
        private EGRPlace _place;
        private readonly MRKSelfContainedPtr<Loading> _mapPreviewLoading;

        public PlaceView()
        {
            _mapPreviewLoading = new MRKSelfContainedPtr<Loading>(
                () => (Loading)_mapPreview.transform.parent.GetComponent<Reference>().GetUsableIntitialized()
            );
        }

        protected override void OnScreenInit()
        {
            _cover = Body.GetElement<RawImage>("Cover/Image");
            _name = Body.GetElement<TextMeshProUGUI>("Layout/Name");
            _tags = Body.GetElement<TextMeshProUGUI>("Layout/Tags");
            _address = Body.GetElement<TextMeshProUGUI>("Layout/Address");
            _description = Body.GetElement<TextMeshProUGUI>("Layout/Desc/Text");
            _mapPreview = Body.GetElement<RawImage>("Layout/MapPreview/Content/Texture");

            Body.GetElement<Button>("Back").onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShow()
        {
            //load map preview?
            LoadMapPreview();
        }

        protected override void OnScreenHide()
        {
            //clear map preview and recycle tex
            _mapPreview.texture = null;

            _mapPreviewLoading.Value.gameObject.SetActive(false);
        }

        private void OnBackClick()
        {
            HideScreen();
        }

        public void SetPlace(EGRPlace place)
        {
            _place = place;
            _name.text = place.Name;
            _tags.text = place.Types.StringifyArray(", ");
            _address.text = place.Address;
            _description.text = place.Type;
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }

        private void LoadMapPreview()
        {
            if (_place == null)
            {
                _mapPreview.texture = null;
                return;
            }

            _mapPreviewLoading.Value.gameObject.SetActive(true);

            TileID tileID = MapUtils.CoordinateToTileId(new Vector2d(_place.Latitude, _place.Longitude), 17);
            Client.Runnable.Run(TileRequestor.Instance.RequestTile(tileID, false, OnReceivedMapPreviewResponse));
        }

        private void OnReceivedMapPreviewResponse(TileFetcherContext ctx)
        {
            _mapPreviewLoading.Value.gameObject.SetActive(false);

            if (ctx.Error)
            {
                MRKLogger.LogError("Cannot load map preview");
                return;
            }

            if (ctx.Texture != null)
            {
                _mapPreview.texture = ctx.MonitoredTexture.Value.Texture;
            }
        }
    }
}
