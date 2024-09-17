//#define MRK_RENDER_TILE_BOUNDS

using MRK.Events;
using MRK.Maps;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MRK
{
    public class Map : BaseBehaviour
    {
        private const int ExtentNorth = 4;
        private const int ExtentSouth = 2;
        private const int ExtentWest = 2;
        private const int ExtentEast = 2;

        [SerializeField]
        private float _zoom;
        [SerializeField]
        private Vector2d _centerLatLng;
        private float _worldRelativeScale;
        private Vector2d _centerMercator;
        private readonly List<TileID> _activeTileIDs;
        private readonly List<TileID> _sortedTileIDs;
        private readonly Dictionary<TileID, TileID> _sortedToActiveIDs;
        private readonly List<Tile> _tiles;
        private readonly Dictionary<TileID, Tile> _idsToTiles;
        private int _initialZoom;
        private int _absoluteZoom;
        [SerializeField]
        private Material _tileMaterial;
        [SerializeField]
        private Texture2D[] _loadingTextures;
        [SerializeField]
        private Texture2D _stationaryTexture;
        private MonitoredTexture _monitoredStationaryTexture;
        [SerializeField]
        private bool _autoInit;
        [SerializeField]
        private string _tileset;
        [SerializeField]
        private float _tileSize = 100f;
        [SerializeField]
        private float[] _desiredTilesetEmission;
        private IMapController _mapController;
        private bool _awaitingMapFullUpdateEvent;
        private readonly List<TileID> _previousTiles;
        private bool _tileDestroyZoomUpdatedDirty;
        [SerializeField]
        private Material _tilePlaneMaterial;
        private readonly List<TilePlane> _activePlanes;
        private readonly HashSet<int> _visibleTiles;
        private int _exN;
        private int _exS;
        private int _exW;
        private int _exE;
        private string _backupTileset;
        private TileID _centerTile;
        private readonly Dictionary<string, MonitoredTexture> _topMostTiles;
        private bool _fetchingTopMost;

        public event Action MapUpdated;
        public event Action<int, int> MapZoomUpdated;
        public event Action MapFullyUpdated;

        public Vector2d CenterLatLng
        {
            get
            {
                return _centerLatLng;
            }
        }

        public float WorldRelativeScale
        {
            get
            {
                return _worldRelativeScale;
            }
        }

        public int InitialZoom
        {
            get
            {
                return _initialZoom;
            }
        }

        public int AbsoluteZoom
        {
            get
            {
                return _absoluteZoom;
            }
        }

        public float Zoom
        {
            get
            {
                return _zoom;
            }
        }

        public Material TileMaterial
        {
            get
            {
                return _tileMaterial;
            }
        }

        public Texture2D LoadingTexture
        {
            get
            {
                return _loadingTextures[(int)Settings.MapStyle];
            }
        }

        public MonitoredTexture StationaryTexture
        {
            get
            {
                return _monitoredStationaryTexture;
            }
        }

        public string Tileset
        {
            get
            {
                return _tileset;
            }
        }

        public float TileSize
        {
            get
            {
                return _tileSize;
            }
        }

        public Vector2d CenterMercator
        {
            get
            {
                return _centerMercator;
            }
        }

        public List<Tile> Tiles
        {
            get
            {
                return _tiles;
            }
        }

        public Material TilePlaneMaterial
        {
            get
            {
                return _tilePlaneMaterial;
            }
        }

        public bool TileDestroyZoomUpdatedDirty
        {
            get
            {
                return _tileDestroyZoomUpdatedDirty;
            }
        }

        public List<TilePlane> ActivePlanes
        {
            get
            {
                return _activePlanes;
            }
        }

        public int PreviousTilesCount
        {
            get
            {
                return _previousTiles.Count;
            }
        }

        public int MaxValidTile
        {
            get; private set;
        }

        public Map()
        {
            _activeTileIDs = new List<TileID>();
            _sortedTileIDs = new List<TileID>();
            _sortedToActiveIDs = new Dictionary<TileID, TileID>();
            _tiles = new List<Tile>();
            _idsToTiles = new Dictionary<TileID, Tile>();
            _previousTiles = new List<TileID>();
            _activePlanes = new List<TilePlane>();
            _visibleTiles = new HashSet<int>();
            _topMostTiles = new Dictionary<string, MonitoredTexture>();

            //default extents
            _exN = ExtentNorth;
            _exS = ExtentSouth;
            _exW = ExtentWest;
            _exE = ExtentEast;
        }

        private void Start()
        {
            //m_TileMaterial.SetTexture("_SecTex", m_LoadingTexture);

            if (_autoInit)
            {
                Initialize(_centerLatLng, Mathf.FloorToInt(_zoom));
            }
        }

        public void AdjustTileSizeForScreen()
        {
            //auto calc tile size
            // 1080 -> 100
            // x -> ?

            bool useHeight = Screen.height < Screen.width;
            float cur = useHeight ? Screen.height : Screen.width;
            float dimBase = useHeight ? 2316f : 1080f;

            _tileSize = 120f + (120f - (cur * 120f / dimBase));
        }

        private void Update()
        {
            //wait for velocity to go down
            if (_awaitingMapFullUpdateEvent)
            {
                float sqrMagnitude = _mapController.GetMapVelocity().sqrMagnitude;
                if (sqrMagnitude <= 0.1f || _zoom < _absoluteZoom - 1 || _zoom > _absoluteZoom + 2)
                {
                    _awaitingMapFullUpdateEvent = false;
                    int newAbsZoom = Mathf.Clamp(Mathf.FloorToInt(_zoom), 0, 21);
                    _tileDestroyZoomUpdatedDirty = _absoluteZoom != newAbsZoom;
                    _absoluteZoom = newAbsZoom;

                    //raise map full update
                    if (MapFullyUpdated != null)
                    {
                        MapFullyUpdated();
                    }

                    _previousTiles.Clear();
                    foreach (TileID tile in _sortedTileIDs)
                    {
                        _previousTiles.Add(_sortedToActiveIDs[tile]);
                    }

                    UpdateScale();
                    UpdatePosition();
                }
            }

#if MRK_DEBUG_FETCHER_LOCK
			Debug.Log(MRKTile.FetcherLock.Recursion);
#endif
        }

        public void Initialize(Vector2d center, int zoom)
        {
            _absoluteZoom = zoom;
            _zoom = _absoluteZoom;
            _initialZoom = _absoluteZoom;
            _centerLatLng = center;

            _monitoredStationaryTexture = new MonitoredTexture(_stationaryTexture, true);

            UpdateTileset();
            UpdateMap(_centerLatLng, _zoom, true);
        }

        public void UpdateTileset()
        {
            string newTileset = Settings.GetCurrentTileset();
            if (_tileset != newTileset)
            {
                _tileset = newTileset;

                if (!_topMostTiles.ContainsKey(newTileset))
                {
                    Client.Runnable.Run(FetchAndStoreTopMostTile(newTileset));
                }
            }
        }

        private IEnumerator FetchAndStoreTopMostTile(string tileset)
        {
            _fetchingTopMost = true;

            FileTileFetcher fileFetcher = MRKSelfContainedPtr<FileTileFetcher>.Global;
            TileID tileID = TileID.TopMost;
            TileFetcher fetcher = fileFetcher.Exists(tileset, tileID, false) ? (TileFetcher)fileFetcher : MRKSelfContainedPtr<RemoteTileFetcher>.Global;

            Reference<UnityWebRequest> webReq = ReferencePool<UnityWebRequest>.Default.Rent();
            TileFetcherContext context = new TileFetcherContext();
            yield return fetcher.Fetch(context, tileset, tileID, webReq, false);

            if (!context.Error && context.Texture != null)
            {
                _topMostTiles[tileset] = new MRKMonitoredTexture(context.Texture, true);
            }

            ReferencePool<UnityWebRequest>.Default.Free(webReq);

            _fetchingTopMost = false;
        }

        public void SetNavigationTileset()
        {
            _backupTileset = _tileset;
            _tileset = "nav";

            UpdatePosition();
        }

        private void UpdateScale()
        {
            Rectd referenceTileRect = MapUtils.TileBounds(MapUtils.CoordinateToTileId(_centerLatLng, _absoluteZoom));
            _worldRelativeScale = _tileSize / (float)referenceTileRect.Size.x;
        }

        public void FitToBounds(Vector2d min, Vector2d max, float padding = 0.2f, bool teleport = true, Reference<float> zoomRef = null)
        {
            float z = Client.ActiveCamera.transform.position.y - transform.position.y;
            Vector3 botLeft = Client.ActiveCamera.ViewportToWorldPoint(new Vector3(padding, padding, z));
            Vector2d botLeftCoords = WorldToGeoPosition(botLeft);
            Vector3 topRight = Client.ActiveCamera.ViewportToWorldPoint(new Vector3(1f - padding, 1f - padding, z));
            Vector2d topRightCoords = WorldToGeoPosition(topRight);

            var targetLonDelta = max.y - min.y;
            var targetLatDelta = max.x - min.x;

            var screenLonDelta = topRightCoords.y - botLeftCoords.y;
            var screenLatDelta = topRightCoords.x - botLeftCoords.x;

            var zoomLatMultiplier = screenLatDelta / targetLatDelta;
            var zoomLonMultiplier = screenLonDelta / targetLonDelta;

            var latZoom = Mathf.Log((float)zoomLatMultiplier, 2);
            var lonZoom = Mathf.Log((float)zoomLonMultiplier, 2);

            float zoom = _zoom + Mathf.Min(latZoom, lonZoom);

            if (zoomRef != null)
            {
                zoomRef.Value = zoom;
            }

            if (teleport)
            {
                _mapController.SetCenterAndZoom((min + max) * 0.5f, zoom);
            }
        }

        private void UpdateTileExtents()
        {
            _activeTileIDs.Clear();
            _sortedTileIDs.Clear();
            _sortedToActiveIDs.Clear();
            _visibleTiles.Clear();

            //set max valid tile
            MaxValidTile = (1 << _absoluteZoom) - 1;
            for (int x = _centerTile.X - _exW; x <= (_centerTile.X + _exE); x++)
            {
                for (int y = (_centerTile.Y - _exN); y <= (_centerTile.Y + _exS); y++)
                {
                    _activeTileIDs.Add(new TileID(_absoluteZoom, x, y, x < 0 || x > MaxValidTile || y < 0 || y > MaxValidTile));
                }
            }

            List<Tile> buf = new List<Tile>();
            foreach (Tile tile in _tiles)
            {
                if (!_activeTileIDs.Contains(tile.ID))
                {
                    //m_TilePool.Free(tile);
                    buf.Add(tile);
                }
            }

            if (_tileDestroyZoomUpdatedDirty)
            {
                _activePlanes.Clear();
            }

            foreach (Tile tile in buf)
            {
                tile.OnDestroy();

                //tell the map interface to dispose markers owned by such tiles
                EventManager.BroadcastEvent(new TileDestroyed(tile, _tileDestroyZoomUpdatedDirty));
                _tiles.Remove(tile);
            }

            _idsToTiles.Clear();
            _tileDestroyZoomUpdatedDirty = false;

            foreach (TileID id in _activeTileIDs)
            {
                TileID sortedID = new TileID(0, id.X - _centerTile.X, id.Y - _centerTile.Y);
                _sortedTileIDs.Add(sortedID);
                _sortedToActiveIDs[sortedID] = id;
            }

            _sortedTileIDs.Sort((x, y) => {
                int sqrMagX = x.Magnitude;
                int sqrMagY = y.Magnitude;

                return sqrMagX.CompareTo(sqrMagY);
            });
        }

        private void UpdatePosition()
        {
            _centerMercator = MapUtils.LatLonToMeters(_centerLatLng);

            TileID centerTile = MapUtils.CoordinateToTileId(_centerLatLng, _absoluteZoom);
            if (_centerTile != centerTile)
            {
                _centerTile = centerTile;
                UpdateTileExtents();
            }

            float scaleFactor = Mathf.Pow(2, _initialZoom - _absoluteZoom);

            int siblingIdx = 0;
            foreach (TileID tileID in _sortedTileIDs)
            {
                TileID realID = _sortedToActiveIDs[tileID];
                Tile tile = _tiles.Find(x => x.ID == realID);
                if (tile == null)
                {
                    tile = new Tile();
                }

                tile.InitTile(this, realID, siblingIdx);

                Rectd rect = tile.Rect.Value;
                Vector3 position = new Vector3((float)(rect.Center.x - _centerMercator.x) * _worldRelativeScale * scaleFactor, 0f,
                    (float)(rect.Center.y - _centerMercator.y) * _worldRelativeScale * scaleFactor);

                tile.Obj.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                tile.Obj.transform.localPosition = position;
                tile.Obj.transform.SetSiblingIndex(siblingIdx++);

                if (!_tiles.Contains(tile))
                {
                    _tiles.Add(tile);
                }

                if (siblingIdx < 6)
                {
                    _visibleTiles.Add(realID.GetHashCode());
                }

                _idsToTiles[realID] = tile;
            }

            transform.position = Vector3.zero;
        }

#if MRK_RENDER_TILE_BOUNDS
		void OnGUI() {
			foreach (MRKTile tile in m_Tiles) {
				RectD r = tile.Rect;

				Vector3 pos = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Min));

				Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos); spos.y = Screen.height - spos.y;

				Vector3 pos2 = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Max));

				Vector3 spos2 = Client.ActiveCamera.WorldToScreenPoint(pos2); spos2.y = Screen.height - spos2.y;

				EGRGL.DrawCircle(spos, 10f, Color.blue);
				EGRGL.DrawCircle(spos2, 20f, Color.white);
				EGRGL.DrawLine(spos, spos2, Color.red, 1.5f);

				EGRGL.DrawBox(spos, spos2, Color.magenta, 1.5f);
			}
		}
#endif

        public void UpdateMap(Vector2d latLon, float zoom, bool force = false)
        {
            if (zoom > 21 || zoom < 0)
            {
                return;
            }

            //zoom didnt change
            if (!force && Math.Abs(_zoom - zoom) <= Mathf.Epsilon && !latLon.IsNotEqual(_centerLatLng))
            {
                return;
            }

            //we are raising the full update event sometime soon
            _awaitingMapFullUpdateEvent = true;

            int newZoom = Mathf.FloorToInt(zoom);
            int oldZoom = AbsoluteZoom;
            bool egrMapZoomUpdated = newZoom != oldZoom;
            if (egrMapZoomUpdated)
            {
                if (MapZoomUpdated != null)
                {
                    MapZoomUpdated(oldZoom, newZoom);
                }
            }

            bool egrMapUpdated = latLon.IsNotEqual(CenterLatLng) || egrMapZoomUpdated || Mathf.Abs(zoom - _zoom) > Mathf.Epsilon;

            // Update map zoom, if it has changed.
            if (Math.Abs(_zoom - zoom) > Mathf.Epsilon)
            {
                _zoom = zoom;
            }

            // Compute difference in zoom. Will be used to calculate correct scale of the map.
            float differenceInZoom = _zoom - _initialZoom;
            bool isAtInitialZoom = differenceInZoom - 0.0 < Mathf.Epsilon;

            //Update center latitude longitude
            var centerLatitudeLongitude = latLon;
            double xDelta = centerLatitudeLongitude.x;
            double zDelta = centerLatitudeLongitude.y;

            //xDelta = xDelta > 0 ? Mathd.Min(xDelta, MRKMapUtils.LATITUDE_MAX) : Mathd.Max(xDelta, -MRKMapUtils.LATITUDE_MAX);
            //zDelta = zDelta > 0 ? Mathd.Min(zDelta, MRKMapUtils.LONGITUDE_MAX) : Mathd.Max(zDelta, -MRKMapUtils.LONGITUDE_MAX);

            //Set Center in Latitude Longitude and Mercator.
            _centerLatLng = new Vector2d(xDelta, zDelta);
            UpdateScale();
            UpdatePosition();

            //Scale the map accordingly.
            if (Math.Abs(differenceInZoom) > Mathf.Epsilon || isAtInitialZoom)
            {
                Vector3 sc = Vector3.one * Mathf.Pow(2, differenceInZoom);
                transform.localScale = sc;
            }

            //EGR
            if (egrMapUpdated)
            {
                if (MapUpdated != null)
                {
                    MapUpdated();
                }
            }

            foreach (TilePlane plane in _activePlanes)
            {
                plane.UpdatePlane();
            }
        }

        public Vector2d WorldToGeoPosition(Vector3 realworldPoint)
        {
            float scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));
            return (transform.InverseTransformPoint(realworldPoint)).GetGeoPosition(_centerMercator, _worldRelativeScale * scaleFactor);
        }

        public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
        {
            float scaleFactor = Mathf.Pow(2f, InitialZoom - AbsoluteZoom);
            var worldPos = MapUtils.GeoToWorldPosition(latitudeLongitude.x, latitudeLongitude.y, _centerMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
            return transform.TransformPoint(worldPos);
        }

        public float GetDesiredTilesetEmission()
        {
            int idx = (int)Settings.MapStyle;
            if (_desiredTilesetEmission.Length > idx)
                return _desiredTilesetEmission[idx];

            return 1f;
        }

        public void SetMapController(IMapController controller)
        {
            _mapController = controller;
        }

        public TileID GetPreviousTileID(int idx)
        {
            return idx >= _previousTiles.Count ? null : _previousTiles[idx];
        }

        public Tile GetTileFromSiblingIndex(int index)
        {
            if (_sortedToActiveIDs.Count <= index)
            {
                return null;
            }

            TileID id = _sortedToActiveIDs[_sortedTileIDs[index]];
            return _idsToTiles.ContainsKey(id) ? _idsToTiles[id] : null;
        }

        public void InvokeUpdateEvent()
        {
            if (MapUpdated != null)
            {
                MapUpdated();
            }
        }

        public bool IsTileVisible(int hash)
        {
            return _visibleTiles.Contains(hash);
        }

        public bool IsAnyTileFetching()
        {
            bool fetching = false;
            foreach (Tile tile in _tiles)
            {
                if (tile.IsFetching)
                {
                    fetching = true;
                    break;
                }
            }

            return fetching;
        }

        public void DestroyAllTiles()
        {
            lock (_tiles)
            {
                foreach (Tile tile in _tiles)
                {
                    tile.OnDestroy();
                    EventManager.BroadcastEvent<TileDestroyed>(new TileDestroyed(tile, false));
                }

                _tiles.Clear();
            }
        }

        public Vector2d ProjectVector(Vector2d v)
        {
            float delta = _mapController.MapRotation.y * Mathf.Deg2Rad;
            return new Vector2d(
                (v.x * Mathd.Cos(delta)) - (v.y * Mathd.Sin(delta)),
                (v.x * Mathd.Sin(delta)) + (v.y * Mathd.Cos(delta))
            );
        }

        public MonitoredTexture GetCurrentTopMostTile()
        {
            MonitoredTexture tex;
            if (!_topMostTiles.TryGetValue(_tileset, out tex))
            {
                if (!_fetchingTopMost)
                {
                    Client.Runnable.Run(FetchAndStoreTopMostTile(_tileset));
                }
            }

            return tex;
        }
    }
}